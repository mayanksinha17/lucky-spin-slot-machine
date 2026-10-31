import re

def patch_scene():
    scene_path = 'Assets/Scenes/MainGame.unity'
    with open(scene_path, 'r', encoding='utf-8') as f:
        text = f.read()

    # 1. Update CanvasScaler (&303)
    text = re.sub(
        r'(--- !u!114 &303\s+MonoBehaviour:.*?m_ScreenMatchMode:\s*)\d+(.*?m_MatchWidthOrHeight:\s*)[0-9.]+',
        r'\g<1>0\g<2>0.5',
        text,
        flags=re.DOTALL
    )

    # 2. Update Anime_Hostess_Panel (&461)
    text = re.sub(
        r'(--- !u!224 &461\s+RectTransform:.*?m_AnchorMin:\s*\{x:\s*)[^,]+,\s*y:\s*[^}]+(\}.*?m_AnchorMax:\s*\{x:\s*)[^,]+,\s*y:\s*[^}]+(\}.*?m_AnchoredPosition:\s*\{x:\s*)[^,]+,\s*y:\s*[^}]+(\}.*?m_SizeDelta:\s*\{x:\s*)[^,]+,\s*y:\s*[^}]+(\}.*?m_Pivot:\s*\{x:\s*)[^,]+,\s*y:\s*[^}]+(\})',
        r'\g<1>0, y: 0\g<2>0, y: 1\g<3>0, y: 0\g<4>620, y: 0\g<5>0, y: 0.5\g<6>',
        text,
        flags=re.DOTALL
    )

    # 3. Update Right_Deco_Panel (&471)
    text = re.sub(
        r'(--- !u!224 &471\s+RectTransform:.*?m_AnchorMin:\s*\{x:\s*)[^,]+,\s*y:\s*[^}]+(\}.*?m_AnchorMax:\s*\{x:\s*)[^,]+,\s*y:\s*[^}]+(\}.*?m_AnchoredPosition:\s*\{x:\s*)[^,]+,\s*y:\s*[^}]+(\}.*?m_SizeDelta:\s*\{x:\s*)[^,]+,\s*y:\s*[^}]+(\}.*?m_Pivot:\s*\{x:\s*)[^,]+,\s*y:\s*[^}]+(\})',
        r'\g<1>1, y: 0\g<2>1, y: 1\g<3>0, y: 0\g<4>600, y: 0\g<5>1, y: 0.5\g<6>',
        text,
        flags=re.DOTALL
    )

    # 4. Update Coin_Stacks_Left (&481)
    text = re.sub(
        r'(--- !u!224 &481\s+RectTransform:.*?m_AnchorMin:\s*\{x:\s*)[^,]+,\s*y:\s*[^}]+(\}.*?m_AnchorMax:\s*\{x:\s*)[^,]+,\s*y:\s*[^}]+(\}.*?m_AnchoredPosition:\s*\{x:\s*)[^,]+,\s*y:\s*[^}]+(\}.*?m_SizeDelta:\s*\{x:\s*)[^,]+,\s*y:\s*[^}]+(\}.*?m_Pivot:\s*\{x:\s*)[^,]+,\s*y:\s*[^}]+(\})',
        r'\g<1>0, y: 0\g<2>0, y: 0\g<3>0, y: 0\g<4>340, y: 340\g<5>0, y: 0\g<6>',
        text,
        flags=re.DOTALL
    )

    # 5. Update Coin_Stacks_Right (&491)
    text = re.sub(
        r'(--- !u!224 &491\s+RectTransform:.*?m_AnchorMin:\s*\{x:\s*)[^,]+,\s*y:\s*[^}]+(\}.*?m_AnchorMax:\s*\{x:\s*)[^,]+,\s*y:\s*[^}]+(\}.*?m_AnchoredPosition:\s*\{x:\s*)[^,]+,\s*y:\s*[^}]+(\}.*?m_SizeDelta:\s*\{x:\s*)[^,]+,\s*y:\s*[^}]+(\}.*?m_Pivot:\s*\{x:\s*)[^,]+,\s*y:\s*[^}]+(\})',
        r'\g<1>1, y: 0\g<2>1, y: 0\g<3>0, y: 0\g<4>340, y: 340\g<5>1, y: 0\g<6>',
        text,
        flags=re.DOTALL
    )

    # 6. Update Balance_Badge (&1001)
    text = re.sub(
        r'(--- !u!224 &1001\s+RectTransform:.*?m_AnchorMin:\s*\{x:\s*)[^,]+,\s*y:\s*[^}]+(\}.*?m_AnchorMax:\s*\{x:\s*)[^,]+,\s*y:\s*[^}]+(\}.*?m_AnchoredPosition:\s*\{x:\s*)[^,]+,\s*y:\s*[^}]+(\}.*?m_SizeDelta:\s*\{x:\s*)[^,]+,\s*y:\s*[^}]+(\}.*?m_Pivot:\s*\{x:\s*)[^,]+,\s*y:\s*[^}]+(\})',
        r'\g<1>0, y: 1\g<2>0, y: 1\g<3>48, y: -36\g<4>320, y: 84\g<5>0, y: 1\g<6>',
        text,
        flags=re.DOTALL
    )

    # 7. Update Bet Deck buttons & labels
    # 7a. Bet_Decrease_Button (&1211)
    text = re.sub(
        r'(--- !u!224 &1211\s+RectTransform:.*?m_AnchoredPosition:\s*\{x:\s*)[^,]+,\s*y:\s*[^}]+(\}.*?m_SizeDelta:\s*\{x:\s*)[^,]+,\s*y:\s*[^}]+(\})',
        r'\g<1>-380, y: 0\g<2>72, y: 72\g<3>',
        text,
        flags=re.DOTALL
    )
    # 7b. Bet_Text (&1221)
    text = re.sub(
        r'(--- !u!224 &1221\s+RectTransform:.*?m_AnchoredPosition:\s*\{x:\s*)[^,]+,\s*y:\s*[^}]+(\}.*?m_SizeDelta:\s*\{x:\s*)[^,]+,\s*y:\s*[^}]+(\})',
        r'\g<1>-230, y: 0\g<2>220, y: 64\g<3>',
        text,
        flags=re.DOTALL
    )
    # 7c. Bet_Increase_Button (&1231)
    text = re.sub(
        r'(--- !u!224 &1231\s+RectTransform:.*?m_AnchoredPosition:\s*\{x:\s*)[^,]+,\s*y:\s*[^}]+(\}.*?m_SizeDelta:\s*\{x:\s*)[^,]+,\s*y:\s*[^}]+(\})',
        r'\g<1>-80, y: 0\g<2>72, y: 72\g<3>',
        text,
        flags=re.DOTALL
    )
    # 7d. Bet_10_Button (&1241)
    text = re.sub(
        r'(--- !u!224 &1241\s+RectTransform:.*?m_AnchoredPosition:\s*\{x:\s*)[^,]+,\s*y:\s*[^}]+(\}.*?m_SizeDelta:\s*\{x:\s*)[^,]+,\s*y:\s*[^}]+(\})',
        r'\g<1>35, y: 0\g<2>90, y: 68\g<3>',
        text,
        flags=re.DOTALL
    )
    # 7e. Bet_50_Button (&1251)
    text = re.sub(
        r'(--- !u!224 &1251\s+RectTransform:.*?m_AnchoredPosition:\s*\{x:\s*)[^,]+,\s*y:\s*[^}]+(\}.*?m_SizeDelta:\s*\{x:\s*)[^,]+,\s*y:\s*[^}]+(\})',
        r'\g<1>145, y: 0\g<2>90, y: 68\g<3>',
        text,
        flags=re.DOTALL
    )
    # 7f. Bet_100_Button (&1261)
    text = re.sub(
        r'(--- !u!224 &1261\s+RectTransform:.*?m_AnchoredPosition:\s*\{x:\s*)[^,]+,\s*y:\s*[^}]+(\}.*?m_SizeDelta:\s*\{x:\s*)[^,]+,\s*y:\s*[^}]+(\})',
        r'\g<1>260, y: 0\g<2>100, y: 68\g<3>',
        text,
        flags=re.DOTALL
    )
    # 7g. Bet_Max_Button (&1271)
    text = re.sub(
        r'(--- !u!224 &1271\s+RectTransform:.*?m_AnchoredPosition:\s*\{x:\s*)[^,]+,\s*y:\s*[^}]+(\}.*?m_SizeDelta:\s*\{x:\s*)[^,]+,\s*y:\s*[^}]+(\})',
        r'\g<1>385, y: 0\g<2>110, y: 68\g<3>',
        text,
        flags=re.DOTALL
    )

    # 8. Spin_Button (&901)
    text = re.sub(
        r'(--- !u!224 &901\s+RectTransform:.*?m_AnchoredPosition:\s*\{x:\s*)[^,]+,\s*y:\s*[^}]+(\}.*?m_SizeDelta:\s*\{x:\s*)[^,]+,\s*y:\s*[^}]+(\})',
        r'\g<1>653, y: -335\g<2>270, y: 270\g<3>',
        text,
        flags=re.DOTALL
    )

    # 9. Increase font sizes for bet text (&1223)
    text = re.sub(
        r'(--- !u!114 &1223\s+MonoBehaviour:.*?m_fontSize:\s*)[0-9.]+',
        r'\g<1>38',
        text,
        flags=re.DOTALL
    )

    # 10. Increase font sizes for preset button labels (&1245, &1255, &1265, &1275)
    for lid in ['1245', '1255', '1265', '1275']:
        text = re.sub(
            r'(--- !u!114 &' + lid + r'\s+MonoBehaviour:.*?m_fontSize:\s*)[0-9.]+',
            r'\g<1>32',
            text,
            flags=re.DOTALL
        )

    # 11. Increase stepper button labels (&1215, &1235)
    for sid in ['1215', '1235']:
        text = re.sub(
            r'(--- !u!114 &' + sid + r'\s+MonoBehaviour:.*?m_fontSize:\s*)[0-9.]+',
            r'\g<1>42',
            text,
            flags=re.DOTALL
        )

    # 12. Increase Spin button text (&913)
    text = re.sub(
        r'(--- !u!114 &913\s+MonoBehaviour:.*?m_fontSize:\s*)[0-9.]+',
        r'\g<1>54',
        text,
        flags=re.DOTALL
    )

    # 13. Ensure Result_Title (&1313) has raycastTarget = 0
    text = re.sub(
        r'(--- !u!114 &1313\s+MonoBehaviour:.*?m_raycastTarget:\s*)\d+',
        r'\g<1>0',
        text,
        flags=re.DOTALL
    )

    with open(scene_path, 'w', encoding='utf-8') as f:
        f.write(text)

    print("MainGame.unity successfully patched with refined layout coordinates.")

if __name__ == '__main__':
    patch_scene()
