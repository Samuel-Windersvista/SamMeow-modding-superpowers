# Dev Log

## 2026-09-13 â€” tarkov-runtime-MCP é¦–ç‰ˆå»ºæˆå¹¶é€šè¿‡çœŸå® server å†’çƒŸ

- å®Œæˆ `.scratch/tarkov-runtime-mcp/` å…¨éƒ¨ 7 å¼ å·¥å•ï¼ˆ01 ç©¿ç”²å¼¹ / 02 å¿«ç…§+profile / 03 sections è¡¥å…¨ / 04 æ—¥å¿—è§£æ / 05 wait_for / 06 æ³¨å†Œ+å†’çƒŸ / 07 ä¼šè¯è·å–+ä¿¡å°ä¿®å¤ï¼‰ã€‚
- å·¥å…·é¢ï¼š`tarkov_server_status` / `tarkov_instances` / `tarkov_snapshot`ï¼ˆprofile/traders/quests/hideout/inventoryï¼‰/ `tarkov_wait_for`ï¼›`raid.*` å ä½å¾… Phase 2 BepInEx Client Bridgeã€‚
- æµ‹è¯•ï¼š142/142 ç»¿ï¼›live å†’çƒŸä¸¤è½®ï¼ˆç¬¬ä¸€è½®æš´éœ²ä¼šè¯ç¼ºå¤±ä¸ä¿¡å°è§£åŒ…ä¸¤ç¼ºé™· â†’ ticket 07 ä¿®å¤ï¼›ç¬¬äºŒè½®å…¨éƒ¨é€šè¿‡ï¼ŒçœŸå®æ•°æ®äººå·¥æ ¸å¯¹ä¸€è‡´ï¼‰ã€‚
- å…³é”®ç»éªŒï¼š5.0 å“åº”ç»Ÿä¸€ `{err,errmsg,data}` ä¿¡å°ï¼›`PHPSESSID` = profileIdï¼Œç»å…ä¼šè¯çš„ `/launcher/v2/profiles` è·å–ï¼›`/launcher/v2/mods` æ˜¯ mod æ¸…å•çš„æœ€ä¼˜è·¯ç”±æºã€‚
- å¾…åŠï¼ˆfollow-upï¼‰ï¼šhideout ç­‰çº§æ”¹è¯» profile `Hideout.Areas`ï¼›traders standing/assort æ”¹è¯» `TradersInfo`ï¼›MCP è¿›ç¨‹éœ€ export `TARKOV_RUNTIME_MCP_USERNAME`ï¼ˆå¯é€‰ PASSWORDï¼‰æ‰èƒ½ç”¨ session å—é™å·¥å…·ã€‚

## 2026-09-13£¨Íí£©¡ª ticket 08£º»îÔ¾ profile ¸úËæ + MO2 VFS È«Á´Â·ÑéÖ¤

- »á»°»ñÈ¡Éı¼¶Îª¹æÔòÁ´£ºÌ½Õë¸úËæ -> username ¾«È· -> µ¥ profile ÁãÅäÖÃ×Ô¶¯ -> ¶à profile ½á¹¹»¯ÆçÒå£¨AMBIGUOUS_PROFILE£©¡£
- ĞÂÔö¿ÉÑ¡ server mod `tools/tarkov-active-probe`£¨ADR-0004£©£ºÖ»¶ÁÂ·ÓÉ `/spt/runtime/active-profiles` ±©Â¶ ProfileActivityService »îÔ¾´°¿Ú¡£
- Àï³Ì±®£º**Ê×´Î¾­ MO2 VFS Æô¶¯ SPT 5.0 server ²¢³É¹¦¼ÓÔØ×Ô¶¨Òå server mod**£¨¼ÙÉè A-1 ÑéÖ¤Í¨¹ı£©£»MCP ÁãÅäÖÃ£¨ÎŞ username£©Ö±Á¬²¢·µ»ØÕæÊµÊı¾İ¡£
