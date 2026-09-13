# 06: é›†æˆæ”¶å°¾â€”â€”OpenCode æ³¨å†Œ + çœŸå® server å†’çƒŸ

**What to build:** ä»"æµ‹è¯•å…¨ç»¿"åˆ°"agent çœŸèƒ½ç”¨"ã€‚å°† MCP ä»¥ `tarkov` åæ³¨å†Œè¿› OpenCode é…ç½®ï¼ˆä¸ spt/mo2 MCP å¹¶åˆ—ï¼‰ï¼›èƒ½åŠ›æ¸…å•ä¸æœ€ç»ˆå®ç°å¯¹é½ï¼›å¯¹çœŸå® SPT 5.0 serverï¼ˆé”šå®š BEM tagï¼‰æ‰§è¡Œäººå·¥/åŠè‡ªåŠ¨å†’çƒŸæ¸…å•ï¼šæ¡æ‰‹ + ç‰ˆæœ¬é—¨ç¦ä¸€æ¬¡ã€å…¨ sections å¿«ç…§ä¸€æ¬¡ã€`wait_for` æˆåŠŸä¸è¶…æ—¶å„ä¸€æ¬¡ã€`raid.*` å ä½è¡Œä¸ºä¸€æ¬¡ï¼›å†’çƒŸç»“æœè®°å½•åˆ° dev-logï¼ˆ`writing-spt-modpack-devlog` æƒ¯ä¾‹ï¼‰ã€‚å†’çƒŸå‘ç°çš„åè®®åå·®ï¼ˆå¦‚ shuffle å®ç°ä¸çœŸå® server ä¸ç¬¦ï¼‰åœ¨æœ¬ç¥¨å†…ä¿®å¤æˆ–é™çº§ä¸º follow-upã€‚

**Blocked by:** 03ï¼ˆå¿«ç…§ sections è¡¥å…¨ï¼‰, 04ï¼ˆmod æ¸…å•ï¼‰, 05ï¼ˆwait_forï¼‰

**Status:** ready-for-agent

- [ ] OpenCode é…ç½®ä¸­ `tarkov` MCP å¯è¢« agent ä¼šè¯å‘ç°ä¸è°ƒç”¨
- [ ] çœŸå® server å†’çƒŸï¼šæ¡æ‰‹æˆåŠŸä¸”ç‰ˆæœ¬ä¸é”šå®š tag ä¸€è‡´
- [ ] çœŸå® server å†’çƒŸï¼šå…¨ sections å¿«ç…§è¿”å›ä¸”æ•°æ®ä¸æ¸¸æˆå†…å®é™…ä¸€è‡´ï¼ˆäººå·¥æ ¸å¯¹ï¼‰
- [ ] çœŸå® server å†’çƒŸï¼š`wait_for` æˆåŠŸè·¯å¾„ä¸è¶…æ—¶è·¯å¾„å„éªŒè¯ä¸€æ¬¡
- [ ] å†’çƒŸç»“æœï¼ˆå«å‘ç°çš„åå·®ä¸å¤„ç½®ï¼‰è®°å½•è¿› dev-log

## Comments

### 2026-09-13 orchestrator ÖĞÆÚ¼ÇÂ¼

- OpenCode ×¢²áÍê³É£º`.mcp.json` ĞÂÔö `tarkov` server£¨node + tools/tarkov-runtime-mcp/dist/index.js£©£¬Óë spt/mo2 ²¢ÁĞ¡£
- stdio Ã°ÑÌÍ¨¹ı£º`tools/list` ·µ»ØÍêÕû¹¤¾ßÃæ£¨server_status / instances / snapshot / wait_for / raid.* Õ¼Î»£©¡£
- ÕæÊµ server Ã°ÑÌ**Î´Ö´ĞĞ**£º127.0.0.1:6969 ÎŞ¼àÌı£¨±¾»úÎŞÔËĞĞÖĞµÄ SPT 5.0 server£©¡£´ı Overseer ÊÖ¶¯Æô¶¯ server ºóÖ´ĞĞÃ°ÑÌÇåµ¥£ºÎÕÊÖ+ÃÅ½û¡¢È« sections ¿ìÕÕ£¨ÈË¹¤ºË¶ÔÓÎÏ·ÄÚÊı¾İ£©¡¢wait_for ³É¹¦/³¬Ê±¸÷Ò»¡¢raid.* Õ¼Î»ĞĞÎª¡£Ã°ÑÌ·¢ÏÖµÄ¼ÙÉèÆ«²î£¨ÈıÌõĞÂÂ·ÓÉµÄ·½·¨/ĞÎ×´¡¢profile/list ·½·¨£©ĞŞ normalizer µ¥ÎÄ¼ş¼´¿É¡£
- dev-log ¼ÇÂ¼ËæÃ°ÑÌ½á¹ûÒ»²¢²¹¼Ç¡£

### 2026-09-13 live smoke µÚÒ»ÂÖ½á¹û£¨²¿·ÖÍ¨¹ı£©

- Í¨¹ı£ºÎÕÊÖ+°æ±¾ÃÅ½û£¨ÕæÊµ°æ±¾ `SPT 5.0.0 (BEM) 49aff9`£¬ÃÅ½û passed£©¡¢instances£¨1 ÊµÀı£©¡¢wait_for ³É¹¦/³¬Ê±Ë«Â·¾¶¡¢raid.* Õ¼Î»¡¢´«Êä²ã£¨HTTPS+zlib+cookie Á´Â·ÕæÊµÅÜÍ¨£©¡£
- ±©Â¶ÎÊÌâ 1£ºsession ÊÜÏŞÂ·ÓÉ£¨profile/list¡¢traderSettings¡¢quest/list£©ÎŞ PHPSESSID Ê±·şÎñ¶ËÅ× "session id provided was empty"£¬MCP ¾²Ä¬¹éÁã¡£Ô´ÂëÈ·ÈÏ cookie Öµ¼´ profileId£»`/launcher/v2/profiles` Ãâ»á»°¿ÉÃ¶¾Ù¡£
- ±©Â¶ÎÊÌâ 2£ºhideout/areas£¨Ãâ»á»°£¬99KB ÕæÊµÊı¾İ£©¾­ MCP ·µ»Ø 0¡ª¡ªÕı³£Æ÷Î´½â `{err,errmsg,data}` ĞÅ·â¡£
- ¸½´ø·¢ÏÖ£º`/launcher/v2/mods` Ãâ»á»°·µ»ØÒÑ¼ÓÔØ mod ÔªÊı¾İ£¨Êµ²â `{}`£¬¸Ã server ÎŞ mod£©£¬ÓÅÓÚÈÕÖ¾¶µµ×¡£
- ´¦ÖÃ£ºÈ«²¿×ªÈë ticket 07 ĞŞ¸´£¬ĞŞ¸´ºóÖØÅÜÃ°ÑÌ¡£
