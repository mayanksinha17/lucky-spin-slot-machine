// Web Audio API Synthesizer & Sound FX Manager for LUCKY SPIN WebGL Build
class WebAudioController {
    constructor() {
        this.ctx = null;
        this.isMuted = false;
        this.volume = 0.8;
        this.buffers = {};
        this.spinLoopSource = null;
        this.initContext();
    }

    initContext() {
        const AudioContext = window.AudioContext || window.webkitAudioContext;
        if (AudioContext) {
            this.ctx = new AudioContext();
        }
    }

    resume() {
        if (this.ctx && this.ctx.state === 'suspended') {
            this.ctx.resume();
        }
    }

    setMuted(muted) {
        this.isMuted = muted;
        if (this.isMuted && this.spinLoopSource) {
            this.stopSpinLoop();
        }
    }

    toggleMute() {
        this.setMuted(!this.isMuted);
        return this.isMuted;
    }

    // Synthesized Sound 1: Button Click
    playButtonClick() {
        if (this.isMuted) return;
        this.resume();
        if (!this.ctx) return;

        const osc = this.ctx.createOscillator();
        const gain = this.ctx.createGain();
        osc.type = 'triangle';
        osc.frequency.setValueAtTime(800, this.ctx.currentTime);
        osc.frequency.exponentialRampToValueAtTime(300, this.ctx.currentTime + 0.05);

        gain.gain.setValueAtTime(0.3 * this.volume, this.ctx.currentTime);
        gain.gain.exponentialRampToValueAtTime(0.01, this.ctx.currentTime + 0.05);

        osc.connect(gain);
        gain.connect(this.ctx.destination);
        osc.start();
        osc.stop(this.ctx.currentTime + 0.05);
    }

    // Synthesized Sound 2: Continuous Reel Spin Loop
    startSpinLoop() {
        if (this.isMuted) return;
        this.resume();
        if (!this.ctx) return;
        this.stopSpinLoop();

        const osc = this.ctx.createOscillator();
        const gain = this.ctx.createGain();
        osc.type = 'sawtooth';
        osc.frequency.setValueAtTime(110, this.ctx.currentTime);

        const lfo = this.ctx.createOscillator();
        const lfoGain = this.ctx.createGain();
        lfo.frequency.setValueAtTime(16, this.ctx.currentTime); // 16 Hz rhythmic clicks
        lfoGain.gain.setValueAtTime(40, this.ctx.currentTime);
        lfo.connect(osc.frequency);

        gain.gain.setValueAtTime(0.08 * this.volume, this.ctx.currentTime);

        osc.connect(gain);
        gain.connect(this.ctx.destination);
        osc.start();
        lfo.start();
        this.spinLoopSource = { osc, lfo };
    }

    stopSpinLoop() {
        if (this.spinLoopSource) {
            try {
                this.spinLoopSource.osc.stop();
                this.spinLoopSource.lfo.stop();
            } catch (e) {}
            this.spinLoopSource = null;
        }
    }

    // Synthesized Sound 3: Reel Mechanical Stop (with pitch step for reel 0, 1, 2)
    playReelStop(reelIndex = 0) {
        if (this.isMuted) return;
        this.resume();
        if (!this.ctx) return;

        const basePitch = 120 * (1 + reelIndex * 0.12);
        const osc = this.ctx.createOscillator();
        const gain = this.ctx.createGain();
        osc.type = 'triangle';
        osc.frequency.setValueAtTime(basePitch, this.ctx.currentTime);
        osc.frequency.exponentialRampToValueAtTime(40, this.ctx.currentTime + 0.08);

        gain.gain.setValueAtTime(0.5 * this.volume, this.ctx.currentTime);
        gain.gain.exponentialRampToValueAtTime(0.01, this.ctx.currentTime + 0.08);

        osc.connect(gain);
        gain.connect(this.ctx.destination);
        osc.start();
        osc.stop(this.ctx.currentTime + 0.08);
    }

    // Synthesized Sound 4: Win Chime (Harmonic Chord)
    playWinChime() {
        if (this.isMuted) return;
        this.resume();
        if (!this.ctx) return;

        const chord = [523.25, 659.25, 783.99, 1046.50]; // C5, E5, G5, C6
        chord.forEach((freq, idx) => {
            const osc = this.ctx.createOscillator();
            const gain = this.ctx.createGain();
            osc.type = 'sine';
            osc.frequency.setValueAtTime(freq, this.ctx.currentTime + idx * 0.04);

            gain.gain.setValueAtTime(0.15 * this.volume, this.ctx.currentTime + idx * 0.04);
            gain.gain.exponentialRampToValueAtTime(0.001, this.ctx.currentTime + idx * 0.04 + 0.6);

            osc.connect(gain);
            gain.connect(this.ctx.destination);
            osc.start(this.ctx.currentTime + idx * 0.04);
            osc.stop(this.ctx.currentTime + idx * 0.04 + 0.6);
        });
    }

    // Synthesized Sound 5: Coin Payout Rollup
    playCoinPayout() {
        if (this.isMuted) return;
        this.resume();
        if (!this.ctx) return;

        for (let i = 0; i < 5; i++) {
            const osc = this.ctx.createOscillator();
            const gain = this.ctx.createGain();
            osc.type = 'sine';
            const f = 900 + (i % 3) * 150;
            osc.frequency.setValueAtTime(f, this.ctx.currentTime + i * 0.06);

            gain.gain.setValueAtTime(0.2 * this.volume, this.ctx.currentTime + i * 0.06);
            gain.gain.exponentialRampToValueAtTime(0.01, this.ctx.currentTime + i * 0.06 + 0.05);

            osc.connect(gain);
            gain.connect(this.ctx.destination);
            osc.start(this.ctx.currentTime + i * 0.06);
            osc.stop(this.ctx.currentTime + i * 0.06 + 0.05);
        }
    }

    // Synthesized Sound 6: Jackpot Fanfare (Brass Orchestral Arpeggio)
    playJackpotFanfare() {
        if (this.isMuted) return;
        this.resume();
        if (!this.ctx) return;

        const notes = [261.63, 329.63, 392.00, 523.25, 659.25, 783.99, 1046.50];
        notes.forEach((freq, idx) => {
            const osc = this.ctx.createOscillator();
            const gain = this.ctx.createGain();
            osc.type = 'sawtooth';
            osc.frequency.setValueAtTime(freq, this.ctx.currentTime + idx * 0.1);

            gain.gain.setValueAtTime(0.2 * this.volume, this.ctx.currentTime + idx * 0.1);
            gain.gain.exponentialRampToValueAtTime(0.01, this.ctx.currentTime + idx * 0.1 + 0.8);

            osc.connect(gain);
            gain.connect(this.ctx.destination);
            osc.start(this.ctx.currentTime + idx * 0.1);
            osc.stop(this.ctx.currentTime + idx * 0.1 + 0.8);
        });
    }

    // Synthesized Sound 7: Win Streak Fanfare
    playStreakFanfare() {
        if (this.isMuted) return;
        this.resume();
        if (!this.ctx) return;

        const notes = [440, 554.37, 659.25, 880]; // A major
        notes.forEach((freq, idx) => {
            const osc = this.ctx.createOscillator();
            const gain = this.ctx.createGain();
            osc.type = 'triangle';
            osc.frequency.setValueAtTime(freq, this.ctx.currentTime + idx * 0.08);

            gain.gain.setValueAtTime(0.2 * this.volume, this.ctx.currentTime + idx * 0.08);
            gain.gain.exponentialRampToValueAtTime(0.01, this.ctx.currentTime + idx * 0.08 + 0.4);

            osc.connect(gain);
            gain.connect(this.ctx.destination);
            osc.start(this.ctx.currentTime + idx * 0.08);
            osc.stop(this.ctx.currentTime + idx * 0.08 + 0.4);
        });
    }
}

