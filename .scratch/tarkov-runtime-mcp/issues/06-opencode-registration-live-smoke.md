# 06: é›†æˆæ”¶å°¾â€”â€”OpenCode æ³¨å†Œ + çœŸå®ž server å†’çƒŸ

**What to build:** ä»?æµ‹è¯•å…¨ç»¿"åˆ?agent çœŸèƒ½ç”?ã€‚å°† MCP ä»?`tarkov` åæ³¨å†Œè¿› OpenCode é…ç½®ï¼ˆä¸Ž spt/mo2 MCP å¹¶åˆ—ï¼‰ï¼›èƒ½åŠ›æ¸…å•ä¸Žæœ€ç»ˆå®žçŽ°å¯¹é½ï¼›å¯¹çœŸå®?SPT 5.0 serverï¼ˆé”šå®?BEM tagï¼‰æ‰§è¡Œäººå·?åŠè‡ªåŠ¨å†’çƒŸæ¸…å•ï¼šæ¡æ‰‹ + ç‰ˆæœ¬é—¨ç¦ä¸€æ¬¡ã€å…¨ sections å¿«ç…§ä¸€æ¬¡ã€`wait_for` æˆåŠŸä¸Žè¶…æ—¶å„ä¸€æ¬¡ã€`raid.*` å ä½è¡Œä¸ºä¸€æ¬¡ï¼›å†’çƒŸç»“æžœè®°å½•åˆ?dev-logï¼ˆ`writing-spt-modpack-devlog` æƒ¯ä¾‹ï¼‰ã€‚å†’çƒŸå‘çŽ°çš„åè®®åå·®ï¼ˆå¦‚ shuffle å®žçŽ°ä¸ŽçœŸå®?server ä¸ç¬¦ï¼‰åœ¨æœ¬ç¥¨å†…ä¿®å¤æˆ–é™çº§ä¸?follow-upã€?
**Blocked by:** 03ï¼ˆå¿«ç…?sections è¡¥å…¨ï¼? 04ï¼ˆmod æ¸…å•ï¼? 05ï¼ˆwait_forï¼?
**Status:** ready-for-agent

- [ ] OpenCode é…ç½®ä¸?`tarkov` MCP å¯è¢« agent ä¼šè¯å‘çŽ°ä¸Žè°ƒç”?- [ ] çœŸå®ž server å†’çƒŸï¼šæ¡æ‰‹æˆåŠŸä¸”ç‰ˆæœ¬ä¸Žé”šå®?tag ä¸€è‡?- [ ] çœŸå®ž server å†’çƒŸï¼šå…¨ sections å¿«ç…§è¿”å›žä¸”æ•°æ®ä¸Žæ¸¸æˆå†…å®žé™…ä¸€è‡´ï¼ˆäººå·¥æ ¸å¯¹ï¼?- [ ] çœŸå®ž server å†’çƒŸï¼š`wait_for` æˆåŠŸè·¯å¾„ä¸Žè¶…æ—¶è·¯å¾„å„éªŒè¯ä¸€æ¬?- [ ] å†’çƒŸç»“æžœï¼ˆå«å‘çŽ°çš„åå·®ä¸Žå¤„ç½®ï¼‰è®°å½•è¿› dev-log

## Comments

### 2026-09-13 orchestrator ÖÐÆÚ¼ÇÂ¼

- OpenCode ×¢²áÍê³É£º`.mcp.json` ÐÂÔö `tarkov` server£¨node + tools/tarkov-runtime-mcp/dist/index.js£©£¬Óë spt/mo2 ²¢ÁÐ¡£
- stdio Ã°ÑÌÍ¨¹ý£º`tools/list` ·µ»ØÍêÕû¹¤¾ßÃæ£¨server_status / instances / snapshot / wait_for / raid.* Õ¼Î»£©¡£
- ÕæÊµ server Ã°ÑÌ**Î´Ö´ÐÐ**£º127.0.0.1:6969 ÎÞ¼àÌý£¨±¾»úÎÞÔËÐÐÖÐµÄ SPT 5.0 server£©¡£´ý Overseer ÊÖ¶¯Æô¶¯ server ºóÖ´ÐÐÃ°ÑÌÇåµ¥£ºÎÕÊÖ+ÃÅ½û¡¢È« sections ¿ìÕÕ£¨ÈË¹¤ºË¶ÔÓÎÏ·ÄÚÊý¾Ý£©¡¢wait_for ³É¹¦/³¬Ê±¸÷Ò»¡¢raid.* Õ¼Î»ÐÐÎª¡£Ã°ÑÌ·¢ÏÖµÄ¼ÙÉèÆ«²î£¨ÈýÌõÐÂÂ·ÓÉµÄ·½·¨/ÐÎ×´¡¢profile/list ·½·¨£©ÐÞ normalizer µ¥ÎÄ¼þ¼´¿É¡£
- dev-log ¼ÇÂ¼ËæÃ°ÑÌ½á¹ûÒ»²¢²¹¼Ç¡£

### 2026-09-13 live smoke µÚÒ»ÂÖ½á¹û£¨²¿·ÖÍ¨¹ý£©

- Í¨¹ý£ºÎÕÊÖ+°æ±¾ÃÅ½û£¨ÕæÊµ°æ±¾ `SPT 5.0.0 (BEM) 49aff9`£¬ÃÅ½û passed£©¡¢instances£¨1 ÊµÀý£©¡¢wait_for ³É¹¦/³¬Ê±Ë«Â·¾¶¡¢raid.* Õ¼Î»¡¢´«Êä²ã£¨HTTPS+zlib+cookie Á´Â·ÕæÊµÅÜÍ¨£©¡£
- ±©Â¶ÎÊÌâ 1£ºsession ÊÜÏÞÂ·ÓÉ£¨profile/list¡¢traderSettings¡¢quest/list£©ÎÞ PHPSESSID Ê±·þÎñ¶ËÅ× "session id provided was empty"£¬MCP ¾²Ä¬¹éÁã¡£Ô´ÂëÈ·ÈÏ cookie Öµ¼´ profileId£»`/launcher/v2/profiles` Ãâ»á»°¿ÉÃ¶¾Ù¡£
- ±©Â¶ÎÊÌâ 2£ºhideout/areas£¨Ãâ»á»°£¬99KB ÕæÊµÊý¾Ý£©¾­ MCP ·µ»Ø 0¡ª¡ªÕý³£Æ÷Î´½â `{err,errmsg,data}` ÐÅ·â¡£
- ¸½´ø·¢ÏÖ£º`/launcher/v2/mods` Ãâ»á»°·µ»ØÒÑ¼ÓÔØ mod ÔªÊý¾Ý£¨Êµ²â `{}`£¬¸Ã server ÎÞ mod£©£¬ÓÅÓÚÈÕÖ¾¶µµ×¡£
- ´¦ÖÃ£ºÈ«²¿×ªÈë ticket 07 ÐÞ¸´£¬ÐÞ¸´ºóÖØÅÜÃ°ÑÌ¡£

### 2026-09-13 live smoke µÚ¶þÂÖ£¨ticket 07 ÐÞ¸´ºó£¬orchestrator Ç×Ñé£©

- È«²¿Í¨¹ý£ºÃÅ½û / instances / È« sections ¿ìÕÕÕæÊµ·ÇÁãÊý¾Ý£¨profile 2¡¢traders 18¡¢quests 39¡¢hideout 28¡¢inventory 431£©/ wait_for Ë«Â·¾¶ / raid Õ¼Î»¡£
- ÒÑÖªÓïÒåÈ±¿Ú×ª follow-up£ºhideout µÈ¼¶Ðè¸Ä¶Á profile `Hideout.Areas`£»traders standing/assort Ðè¸Ä¶Á profile `TradersInfo` + assort Â·ÓÉ£»password µÇÂ¼Â·¾¶Î´¾­ live ÑéÖ¤¡£
- ÑéÊÕ±ê×¼È«²¿´ï³É£¬ticket 06 ±Õ»·¡£
