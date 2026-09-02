include Makefile.helpers
modname = MeaningfulWeaponMasteries
dependencies =

assemble:
	rm -f -r export
	mkdir -p export/$(pluginpath)/$(modname)
	cp -u $(dllpath)/$(modname).dll export/$(pluginpath)/$(modname)/

forceinstall:
	make assemble
	rm -r -f $(gamepath)/$(pluginpath)/$(modname)
	cp -u -r export/* $(gamepath)

play:
	(make install && cd .. && make play)
