// LUCKY SPIN – Ultimate Slot Machine
// WebGL / HTML5 Engine Mirroring C# Game Architecture

const SYMBOLS = [
    { type: 'Cherry', name: 'Cherry', multiplier: 2, weight: 35, icon: 'Assets/Art/Symbols/symbol_cherry.png' },
    { type: 'Bell', name: 'Bell', multiplier: 5, weight: 25, icon: 'Assets/Art/Symbols/symbol_bell.png' },
    { type: 'Bar', name: 'BAR', multiplier: 10, weight: 18, icon: 'Assets/Art/Symbols/symbol_bar.png' },
    { type: 'Seven', name: 'Lucky 7', multiplier: 25, weight: 12, icon: 'Assets/Art/Symbols/symbol_seven.png' },
    { type: 'Diamond', name: 'Diamond', multiplier: 100, weight: 6, icon: 'Assets/Art/Symbols/symbol_diamond.png' },
    { type: 'Wild', name: 'Wild', multiplier: 50, weight: 4, isWild: true, icon: 'Assets/Art/Symbols/symbol_wild.png' }
];

// 1. Payout & Virtual Credit Manager
class PayoutManager {
    constructor(startingBalance = 1000, initialBet = 10, minBet = 10, maxBet = 500, betStep = 10) {
        this.currentBalance = startingBalance;
        this.startingBalance = startingBalance;
        this.currentBet = initialBet;
        this.minBet = minBet;
        this.maxBet = maxBet;
        this.betStep = betStep;
        this.activeTransaction = null;
        this.totalBetsPlaced = 0;
        this.totalPayoutsAwarded = 0;
        this.totalSpinsDebited = 0;
    }

    hasSufficientCredits() {
        return this.currentBalance >= this.currentBet;
    }

    tryDeductBet() {
        if (this.activeTransaction !== null) {
            console.warn('[PayoutManager] Rejected: Active unsettled transaction in progress.');
            return false;
        }
        if (!this.hasSufficientCredits()) {
            console.warn('[PayoutManager] Insufficient balance for bet ' + this.currentBet);
            return false;
        }

        this.currentBalance -= this.currentBet;
        this.totalBetsPlaced += this.currentBet;
        this.totalSpinsDebited++;
        this.activeTransaction = {
            id: this.totalSpinsDebited,
            bet: this.currentBet,
            time: Date.now(),
            settled: false
        };
        return true;
    }

    awardPayout(multiplier, streakMultiplier = 1.0) {
        if (!this.activeTransaction) {
            console.warn('[PayoutManager] Attempted payout without active transaction.');
            return 0;
        }

        let basePayout = this.activeTransaction.bet * multiplier;
        let finalPayout = Math.floor(basePayout * streakMultiplier);

        this.currentBalance += finalPayout;
        this.totalPayoutsAwarded += finalPayout;
        this.activeTransaction.payout = finalPayout;
        this.activeTransaction.settled = true;
        this.activeTransaction = null;

        return finalPayout;
    }

    setBet(amount) {
        if (this.activeTransaction) return false;
        let clamped = Math.max(this.minBet, Math.min(this.maxBet, amount));
        let rem = (clamped - this.minBet) % this.betStep;
        clamped -= rem;
        this.currentBet = clamped;
        return true;
    }

    increaseBet() {
        if (this.currentBet + this.betStep <= this.maxBet) {
            return this.setBet(this.currentBet + this.betStep);
        }
        return false;
    }

    decreaseBet() {
        if (this.currentBet - this.betStep >= this.minBet) {
            return this.setBet(this.currentBet - this.betStep);
        }
        return false;
    }

    autoAdjustBetToAffordable() {
        if (this.activeTransaction || this.currentBalance >= this.currentBet) return false;
        if (this.currentBalance < this.minBet) return false;
        let steps = Math.floor((this.currentBalance - this.minBet) / this.betStep);
        let affordable = this.minBet + (steps * this.betStep);
        return this.setBet(affordable);
    }
}

// 2. Random Outcome Generator
class RandomOutcomeGenerator {
    constructor() {
        this.useWeighted = true;
    }

    generateOutcome() {
        return [
            this.selectSymbol(),
            this.selectSymbol(),
            this.selectSymbol()
        ];
    }

    selectSymbol() {
        if (!this.useWeighted) {
            const idx = Math.floor(Math.random() * SYMBOLS.length);
            return SYMBOLS[idx];
        }

        const totalWeight = SYMBOLS.reduce((sum, s) => sum + s.weight, 0);
        let roll = Math.random() * totalWeight;
        let accumulated = 0;
        for (let s of SYMBOLS) {
            accumulated += s.weight;
            if (roll < accumulated) return s;
        }
        return SYMBOLS[0];
    }
}

// 3. Winning Evaluator (with Modular Wild Symbol)
class WinningEvaluator {
    evaluate(symbols, enableWild = true) {
        const [s1, s2, s3] = symbols;

        // Exact 3 identical match
        if (s1.type === s2.type && s2.type === s3.type) {
            return {
                isWin: true,
                symbol: s1,
                multiplier: s1.multiplier,
                isWildMatch: s1.isWild || false,
                isWildAssisted: false
            };
        }

        // Modular Wild substitution logic
        if (enableWild) {
            const wilds = symbols.filter(s => s.isWild).length;
            const nonWilds = symbols.filter(s => !s.isWild);

            if (wilds === 1 && nonWilds.length === 2) {
                if (nonWilds[0].type === nonWilds[1].type) {
                    return {
                        isWin: true,
                        symbol: nonWilds[0],
                        multiplier: nonWilds[0].multiplier,
                        isWildAssisted: true
                    };
                }
            } else if (wilds === 2 && nonWilds.length === 1) {
                return {
                    isWin: true,
                    symbol: nonWilds[0],
                    multiplier: nonWilds[0].multiplier,
                    isWildAssisted: true
                };
            }
        }

        return { isWin: false, symbol: null, multiplier: 0, isWildAssisted: false };
    }
}

// 4. Reel Controller
class ReelController {
    constructor(elementId, reelIndex) {
        this.element = document.getElementById(elementId);
        this.reelIndex = reelIndex;
        this.slots = this.element.querySelectorAll('.symbol-slot img');
        this.isSpinning = false;
        this.currentOffset = 0;
        this.slotHeight = 116.66;
        this.finalSymbol = SYMBOLS[0];
        this.animationFrame = null;
        this.setupInitialSymbols();
    }

    setupInitialSymbols() {
        this.slots[0].src = SYMBOLS[(this.reelIndex + 1) % SYMBOLS.length].icon;
        this.slots[1].src = SYMBOLS[this.reelIndex % SYMBOLS.length].icon; // Center payline
        this.slots[2].src = SYMBOLS[(this.reelIndex + 2) % SYMBOLS.length].icon;
    }

    spin() {
        this.isSpinning = true;
    }

    stopAt(targetSymbol, onComplete) {
        this.finalSymbol = targetSymbol;
        const totalDuration = 1000; // ms
        const startTime = performance.now();

        const animate = (currentTime) => {
            const elapsed = currentTime - startTime;
            const progress = Math.min(1.0, elapsed / totalDuration);

            // EaseOutBack: overshoot bounce settling curve
            const c1 = 1.70158;
            const c3 = c1 + 1;
            const easeOutBack = 1 + c3 * Math.pow(progress - 1, 3) + c1 * Math.pow(progress - 1, 2);

            // Rapid symbol blur during motion
            if (progress < 0.8) {
                const randomSym = SYMBOLS[Math.floor(Math.random() * SYMBOLS.length)];
                this.slots[1].src = randomSym.icon;
            } else {
                // Snap to predetermined final symbol
                this.slots[1].src = this.finalSymbol.icon;
                const prev = SYMBOLS[(SYMBOLS.indexOf(this.finalSymbol) + SYMBOLS.length - 1) % SYMBOLS.length];
                const next = SYMBOLS[(SYMBOLS.indexOf(this.finalSymbol) + 1) % SYMBOLS.length];
                this.slots[0].src = prev.icon;
                this.slots[2].src = next.icon;
            }

            // Bounce transform
            const bounceY = (1 - easeOutBack) * 30;
            this.element.style.transform = `translateY(${bounceY}px)`;

            if (progress < 1.0) {
                this.animationFrame = requestAnimationFrame(animate);
            } else {
                this.element.style.transform = 'translateY(0px)';
                this.isSpinning = false;
                if (onComplete) onComplete();
            }
        };

        this.animationFrame = requestAnimationFrame(animate);
    }
}

// 5. Coin Particle Fountain Engine
class ParticleEngine {
    constructor(canvasId) {
        this.canvas = document.getElementById(canvasId);
        this.ctx = this.canvas ? this.canvas.getContext('2d') : null;
        this.particles = [];
        this.resize();
        window.addEventListener('resize', () => this.resize());
        this.animate = this.animate.bind(this);
        requestAnimationFrame(this.animate);
    }

    resize() {
        if (!this.canvas) return;
        this.canvas.width = this.canvas.parentElement.clientWidth;
        this.canvas.height = this.canvas.parentElement.clientHeight;
    }

    spawnCoinFountain(x, y, count = 50) {
        for (let i = 0; i < count; i++) {
            const angle = -Math.PI / 2 + (Math.random() - 0.5) * 1.2;
            const speed = 400 + Math.random() * 500;
            this.particles.push({
                x: x || this.canvas.width / 2,
                y: y || this.canvas.height / 2 + 50,
                vx: Math.cos(angle) * speed,
                vy: Math.sin(angle) * speed,
                gravity: 980,
                radius: 10 + Math.random() * 8,
                rotation: Math.random() * Math.PI * 2,
                vRot: (Math.random() - 0.5) * 10,
                life: 1.0,
                decay: 0.5 + Math.random() * 0.4
            });
        }
    }

    animate(timestamp) {
        if (this.ctx) {
            this.ctx.clearRect(0, 0, this.canvas.width, this.canvas.height);

            const dt = 0.016;
            for (let i = this.particles.length - 1; i >= 0; i--) {
                const p = this.particles[i];
                p.vy += p.gravity * dt;
                p.x += p.vx * dt;
                p.y += p.vy * dt;
                p.rotation += p.vRot * dt;
                p.life -= p.decay * dt;

                if (p.life <= 0) {
                    this.particles.splice(i, 1);
                    continue;
                }

                // Render golden coin with shiny rim
                this.ctx.save();
                this.ctx.translate(p.x, p.y);
                this.ctx.rotate(p.rotation);
                this.ctx.globalAlpha = Math.max(0, p.life);

                this.ctx.beginPath();
                this.ctx.arc(0, 0, p.radius, 0, Math.PI * 2);
                this.ctx.fillStyle = '#ffc400';
                this.ctx.fill();

                this.ctx.lineWidth = 3;
                this.ctx.strokeStyle = '#fff08a';
                this.ctx.stroke();

                this.ctx.restore();
            }
        }
        requestAnimationFrame(this.animate);
    }
}

// 6. Master GameManager
class GameManager {
    constructor() {
        this.payoutManager = new PayoutManager(1000, 10);
        this.outcomeGenerator = new RandomOutcomeGenerator();
        this.evaluator = new WinningEvaluator();
        this.audio = new WebAudioController();
        this.particles = new ParticleEngine('particle-canvas');
        this.reels = [
            new ReelController('reel-strip-1', 0),
            new ReelController('reel-strip-2', 1),
            new ReelController('reel-strip-3', 2)
        ];

        this.state = 'Idle'; // Idle, Spinning, Evaluating, Payout, InsufficientFunds
        this.currentPredeterminedOutcomes = null;
        this.lastResult = null;
        this.winStreak = 0;
        this.history = [];
        this.autoSpinRemaining = 0;
        this.autoSpinActive = false;

        this.bindUI();
        this.updateUI();
        console.log('[GameManager] LUCKY SPIN Slot Machine initialized.');
    }

    getState() { return this.state; }
    getBalance() { return this.payoutManager.currentBalance; }
    getCurrentBet() { return this.payoutManager.currentBet; }
    getLastResult() { return this.lastResult; }
    getHistory() { return this.history; }

    bindUI() {
        document.getElementById('btn-spin').addEventListener('click', () => this.requestSpin());
        document.getElementById('btn-bet-plus').addEventListener('click', () => {
            this.payoutManager.increaseBet();
            this.audio.playButtonClick();
            this.updateUI();
        });
        document.getElementById('btn-bet-minus').addEventListener('click', () => {
            this.payoutManager.decreaseBet();
            this.audio.playButtonClick();
            this.updateUI();
        });
        document.getElementById('btn-max-bet').addEventListener('click', () => {
            this.payoutManager.setBet(this.payoutManager.maxBet);
            this.audio.playButtonClick();
            this.updateUI();
        });

        // Quick Bet Buttons
        document.querySelectorAll('.btn-quick-bet').forEach(btn => {
            btn.addEventListener('click', (e) => {
                const betVal = parseInt(e.target.getAttribute('data-bet'), 10);
                if (betVal) {
                    this.payoutManager.setBet(betVal);
                    this.audio.playButtonClick();
                    this.updateUI();
                }
            });
        });

        // Audio Mute Toggle
        document.getElementById('btn-mute').addEventListener('click', () => {
            const isMuted = this.audio.toggleMute();
            document.getElementById('btn-mute').textContent = isMuted ? '🔇' : '🔊';
        });

        // History Drawer Toggle
        document.getElementById('btn-history').addEventListener('click', () => {
            document.getElementById('history-drawer').classList.toggle('open');
            this.audio.playButtonClick();
        });
        document.getElementById('btn-close-history').addEventListener('click', () => {
            document.getElementById('history-drawer').classList.remove('open');
            this.audio.playButtonClick();
        });

        // Auto-Spin Buttons
        document.getElementById('btn-auto-spin').addEventListener('click', () => {
            if (this.autoSpinActive) {
                this.stopAutoSpin();
            } else {
                this.startAutoSpin(10);
            }
        });

        // Jackpot Modal Dismiss
        document.getElementById('jackpot-modal').addEventListener('click', () => {
            document.getElementById('jackpot-modal').style.display = 'none';
        });
    }

    requestSpin() {
        if (this.state !== 'Idle') return;

        // 1. Credit Validation
        if (!this.payoutManager.hasSufficientCredits()) {
            this.state = 'InsufficientFunds';
            this.showBanner('INSUFFICIENT CREDITS', 'Please lower your bet or reload demo balance.');
            return;
        }

        // 2. Atomic Credit Deduction
        if (!this.payoutManager.tryDeductBet()) return;

        // 3. RNG Outcome Generation BEFORE visual presentation
        this.currentPredeterminedOutcomes = this.outcomeGenerator.generateOutcome();
        this.lastResult = this.evaluator.evaluate(this.currentPredeterminedOutcomes, true);

        console.log(`[RNG] Outcome: [${this.currentPredeterminedOutcomes.map(s => s.name).join(', ')}] | Win: ${this.lastResult.isWin}`);

        // 4. Update State to Spinning & Start Audio/UI
        this.state = 'Spinning';
        this.audio.playButtonClick();
        this.audio.startSpinLoop();
        this.hideBanner();
        this.documentHighlightPayline(false);
        this.updateUI();

        // 5. Trigger Reel Animation with Sequential Stopping
        this.reels.forEach(r => r.spin());

        // Staggered stop delays: Reel 1 (1.2s), Reel 2 (1.6s), Reel 3 (2.0s)
        setTimeout(() => {
            this.reels[0].stopAt(this.currentPredeterminedOutcomes[0], () => {
                this.audio.playReelStop(0);
            });
        }, 1200);

        setTimeout(() => {
            this.reels[1].stopAt(this.currentPredeterminedOutcomes[1], () => {
                this.audio.playReelStop(1);
            });
        }, 1600);

        setTimeout(() => {
            this.reels[2].stopAt(this.currentPredeterminedOutcomes[2], () => {
                this.audio.playReelStop(2);
                this.handleSpinCompleted();
            });
        }, 2000);
    }

    handleSpinCompleted() {
        this.audio.stopSpinLoop();
        this.state = 'Evaluating';

        let streakMult = 1.0;
        if (this.lastResult.isWin) {
            this.winStreak++;
            if (this.winStreak === 2) streakMult = 1.25;
            else if (this.winStreak === 3) streakMult = 1.50;
            else if (this.winStreak >= 4) streakMult = 2.00;
        } else {
            this.winStreak = 0;
        }

        const payout = this.payoutManager.awardPayout(this.lastResult.multiplier, streakMult);

        // Record Spin in History
        this.history.unshift({
            spin: this.payoutManager.totalSpinsDebited,
            symbols: this.currentPredeterminedOutcomes.map(s => s.name),
            bet: this.payoutManager.currentBet,
            payout: payout,
            isWin: this.lastResult.isWin,
            balance: this.payoutManager.currentBalance
        });
        this.renderHistory();

        if (this.lastResult.isWin) {
            this.state = 'Payout';
            this.documentHighlightPayline(true);

            const isJackpot = this.lastResult.symbol && (this.lastResult.symbol.type === 'Diamond' || payout >= 1000);
            if (isJackpot) {
                this.triggerJackpotCelebration(payout);
            } else {
                this.audio.playWinChime();
                this.audio.playCoinPayout();
                this.particles.spawnCoinFountain();
                let msg = `WIN: +${payout} G`;
                if (streakMult > 1.0) msg += ` (${streakMult}x STREAK BOOST!)`;
                this.showBanner(this.lastResult.symbol.name.toUpperCase() + ' MATCH!', msg);
            }
        } else {
            this.showBanner('NO MATCH', 'Try again!');
        }

        // Auto adjust bet if credits lower than current bet
        this.payoutManager.autoAdjustBetToAffordable();

        setTimeout(() => {
            this.state = 'Idle';
            this.updateUI();

            // Handle next auto-spin if active
            if (this.autoSpinActive) {
                if (this.autoSpinRemaining > 0 && this.payoutManager.hasSufficientCredits()) {
                    this.autoSpinRemaining--;
                    setTimeout(() => this.requestSpin(), 800);
                } else {
                    this.stopAutoSpin();
                }
            }
        }, 1200);
    }

    triggerJackpotCelebration(payout) {
        this.audio.playJackpotFanfare();
        this.particles.spawnCoinFountain(null, null, 100);
        const modal = document.getElementById('jackpot-modal');
        document.getElementById('jackpot-amount').textContent = `+${payout} G`;
        modal.style.display = 'flex';
        this.showBanner('💥 JACKPOT! 💥', `MASSIVE WIN: +${payout} G!`);
    }

    startAutoSpin(count = 10) {
        if (!this.payoutManager.hasSufficientCredits()) {
            this.showBanner('INSUFFICIENT CREDITS', 'Cannot start auto-spin.');
            return;
        }
        this.autoSpinActive = true;
        this.autoSpinRemaining = count;
        document.getElementById('btn-auto-spin').textContent = `STOP (${this.autoSpinRemaining})`;
        document.getElementById('btn-auto-spin').classList.add('active');
        this.requestSpin();
    }

    stopAutoSpin() {
        this.autoSpinActive = false;
        this.autoSpinRemaining = 0;
        document.getElementById('btn-auto-spin').textContent = 'AUTO SPIN';
        document.getElementById('btn-auto-spin').classList.remove('active');
    }

    documentHighlightPayline(active) {
        const payline = document.getElementById('payline');
        if (payline) {
            if (active) payline.classList.add('highlight');
            else payline.classList.remove('highlight');
        }
    }

    showBanner(title, amount) {
        const banner = document.getElementById('win-banner');
        document.getElementById('banner-title').textContent = title;
        document.getElementById('banner-amount').textContent = amount;
        banner.classList.add('show');
    }

    hideBanner() {
        const banner = document.getElementById('win-banner');
        banner.classList.remove('show');
    }

    updateUI() {
        document.getElementById('balance-display').textContent = this.payoutManager.currentBalance + ' G';
        document.getElementById('bet-display').textContent = this.payoutManager.currentBet + ' G';

        const isSpinning = this.state !== 'Idle';
        document.getElementById('btn-spin').disabled = isSpinning;
        document.getElementById('btn-bet-plus').disabled = isSpinning || this.payoutManager.currentBet >= this.payoutManager.maxBet;
        document.getElementById('btn-bet-minus').disabled = isSpinning || this.payoutManager.currentBet <= this.payoutManager.minBet;
        document.getElementById('btn-max-bet').disabled = isSpinning;

        // Quick Bet highlight
        document.querySelectorAll('.btn-quick-bet').forEach(btn => {
            const betVal = parseInt(btn.getAttribute('data-bet'), 10);
            if (betVal === this.payoutManager.currentBet) {
                btn.classList.add('active');
            } else {
                btn.classList.remove('active');
            }
        });

        // Win streak badge
        const streakBadge = document.getElementById('streak-badge');
        if (this.winStreak >= 2) {
            streakBadge.style.display = 'block';
            streakBadge.textContent = `🔥 ${this.winStreak}x WIN STREAK!`;
        } else {
            streakBadge.style.display = 'none';
        }
    }

    renderHistory() {
        const list = document.getElementById('history-items');
        if (!list) return;
        list.innerHTML = '';
        this.history.slice(0, 15).forEach(item => {
            const div = document.createElement('div');
            div.className = 'history-item ' + (item.isWin ? 'win' : 'loss');
            div.innerHTML = `
                <div>
                    <strong>#${item.spin}</strong> [${item.symbols.join(' ')}]
                </div>
                <div style="color: ${item.isWin ? '#ffd700' : '#a89db8'}">
                    ${item.isWin ? '+' + item.payout + ' G' : '-' + item.bet + ' G'}
                </div>
            `;
            list.appendChild(div);
        });

        // Metrics summary
        const totalWins = this.history.filter(h => h.isWin).length;
        const winRate = this.history.length > 0 ? Math.round((totalWins / this.history.length) * 100) : 0;
        document.getElementById('history-summary').textContent = `Spins: ${this.history.length} | Wins: ${totalWins} (${winRate}%)`;
    }

    resetBalance(amount = 1000) {
        this.payoutManager.currentBalance = amount;
        this.updateUI();
    }
}

// Instantiate on DOMContentLoaded
window.addEventListener('DOMContentLoaded', () => {
    window.gameManager = new GameManager();
});
