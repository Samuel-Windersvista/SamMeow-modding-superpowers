# 08: æ´»è·ƒ profile è·Ÿéšï¼ˆA: è‡ªåŠ¨é€‰æ‹©è§„åˆ™ + B: å¯é€‰æ´»è·ƒæŽ¢é’?modï¼?
**What to build:** ä¼šè¯èŽ·å–ä»?å¿…é¡»é…ç½® username"å‡çº§ä¸ºè·ŸéšçŽ©å®¶å®žé™…ä½¿ç”¨çš„ profileï¼?
- **Aï¼ˆå¿…åšï¼Œçº?MCP ä¾§ï¼‰**ï¼š`/launcher/v2/profiles` è¿”å›žæ°å¥½ 1 ä¸?profile æ—¶é›¶é…ç½®è‡ªåŠ¨é€‰ç”¨ï¼›å¤š profile ä¸”æœªé…ç½® username æ—¶è¿”å›žç»“æž„åŒ– `AMBIGUOUS_PROFILE`ï¼ˆåˆ—å‡ºå€™é€?usernameï¼‰ï¼›å·²é…ç½?username ç»´æŒç²¾ç¡®åŒ¹é…ã€‚æŽ¢æµ‹åˆ°æ´»è·ƒæŽ¢é’ˆè·¯ç”±ï¼ˆBï¼‰æ—¶ä¼˜å…ˆä½¿ç”¨æ´»è·ƒ profileã€?- **Bï¼ˆå¯é€‰å¢žå¼ºï¼Œæ¸¸æˆä¾§å¾® modï¼?*ï¼šSPT 5.0 C# server modï¼ˆIModMetadata + StaticRouterï¼‰ï¼Œåªæ³¨å†Œä¸€ä¸ªåªè¯»è·¯ç”?`/spt/runtime/active-profiles`ï¼Œè¿”å›?`ProfileActivityService.GetActiveProfileIdsWithinMinutes(N)` çš?profileId åˆ—è¡¨ã€‚MCP æ¡æ‰‹æ—¶æŽ¢æµ‹è¯¥è·¯ç”±ï¼šå¯ç”¨åˆ™è·Ÿéšæ´»è·ƒ profileï¼Œä¸å¯ç”¨é€€å›?A è§„åˆ™ï¼ˆæ¸è¿›å¢žå¼ºï¼Œä¸å­˜åœ¨ä¸æŠ¥é”™ï¼‰ã€‚éƒ¨ç½²æ–¹å¼ä¸Ž MO2 overlay éªŒè¯ï¼ˆå‡è®?A-1ï¼‰åœ¨æ‰§è¡Œæ—¶ç¡®è®¤ã€?
**Blocked by:** Noneï¼ˆåŸºäº?mainï¼›A ä¸?B å¯åˆ†åˆ«éªŒè¯ï¼‰

**Status:** ready-for-human

- [ ] å?profile é›¶é…ç½®è‡ªåŠ¨é€‰ç”¨ï¼Œsession å—é™å·¥å…·ç›´æŽ¥å¯ç”¨
- [ ] å¤?profile æœªé…ç½?username è¿”å›ž `AMBIGUOUS_PROFILE` ä¸”åˆ—å‡ºå€™é€?- [ ] å·²é…ç½?username æ—¶ç²¾ç¡®åŒ¹é…è¡Œä¸ºä¸å›žé€€
- [ ] æŽ¢é’ˆè·¯ç”±å¯ç”¨æ—¶ä¼˜å…ˆè·Ÿéšæ´»è·?profileï¼›ä¸å¯ç”¨æ—¶é™é»˜é€€å›?A è§„åˆ™
- [ ] æŽ¢é’ˆ mod åœ¨çœŸå®?5.0 server åŠ è½½å¹¶æˆåŠŸè¿”å›žæ´»è·?profileï¼ˆlive éªŒè¯ï¼?- [ ] å…¨é‡æµ‹è¯•ç»¿ï¼ˆå«æ—¢æœ?142ï¼?
## Comments

### 2026-09-13 orchestrator ½»¸¶¼ÇÂ¼

- A ²¿·Ö£¨MCP ²à¹æÔòÁ´£©£ºÌ½ÕëÓÅÏÈ -> username ¾«È· -> µ¥ profile ×Ô¶¯ -> ¶à profile AMBIGUOUS_PROFILE¡£148/148 ²âÊÔÂÌ¡£
- B ²¿·Ö£¨Ì½Õë mod£©£ºtools/tarkov-active-probe ¹¹½¨ 0 ´í 0 ¾¯¡£
- **A-1 ¼ÙÉèÑéÖ¤Í¨¹ý**£º¾­ MO2£¨ÊµÀý SPT5£¬gamePath E:\Game\EFT_Offline\SPT_5xx£¬base_directory Inescapable Tarkov£©VFS Æô¶¯ server£¬Ì½Õë mod ³É¹¦¼ÓÔØ£¬`/spt/runtime/active-profiles` ·µ»ØÔ¤ÆÚÐÎ×´¡£
- ÁãÅäÖÃ live ÑéÖ¤£ºÎÞ TARKOV_RUNTIME_MCP_USERNAME Ê±¿ìÕÕ×Ô¶¯Ñ¡ÓÃÎ¨Ò» profile£¨auto-single£©£¬·µ»ØÕæÊµÊý¾Ý¡£Ì½Õë active-probe Â·¾¶µÄÍêÕû live ÑéÖ¤´ýÓÎÏ·¿Í»§¶ËÊµ¼ÊÓÎÍæ£¨µ±Ç°ÎÞ¿Í»§¶ËÇëÇó£¬Ì½Õë·µ»Ø¿ÕÁÐ±í²¢ÕýÈ·ÍË»Ø A ¹æÔò£©¡£
- »·¾³±ä¸ü¼ÇÂ¼£¨²¿ÊðºÛ¼££©£º
  1. ¸´ÖÆ `ModOrganizer.ini` µ½ÊµÀý base_directory ¸ù£¨ÃÖ²¹ mo2-mcp ²»½âÎö base_directory ÖØ¶¨ÏòµÄÈ±¿Ú£»MO2 ×ÔÉí²»¶Á¸ÃÎ»ÖÃ£©£»
  2. `.mo2-mcp.json`£¨permission_ceiling=full-control£©Ð´ÈëÊµÀý base_directory Óë LOCALAPPDATA ÊµÀýÄ¿Â¼£»
  3. modlist.txt ÊÖ¶¯×·¼Ó `+TarkovActiveProbe`£¨ÀëÏß´´½¨Â·¾¶£¬MO2 Ë¢ÐÂºó×ÔÇ¢£©£»
  4. MO2 Æô¶¯¹ýÒ»´Î£¨SPT-Organizer v2.5.2£©£¬ÆÚ¼ä±ÀÀ£¹ýÒ»´ÎºóÖØÆôÎÈ¶¨¡£
- ·¢ÏÖ£ºmo2-mcp ¶ÔÈ«¾ÖÊµÀý£¨ini Óë base_directory ·ÖÀë£©Ö§³ÖÓÐÈ±¿Ú£»broker Î´×°£¨pipeConnected=false£©£¬create_mod Ðè live broker£¬ÀëÏßÂ·¾¶ÎªÊÖÐ´ modlist¡£½¨Òé¼ÇÎª mo2-mcp µÄ¸Ä½øÏî¡£
