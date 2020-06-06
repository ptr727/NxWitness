# How do I get wildcards working?
#dockerfiles := $(addsuffix .dockerfile, $(basename $(wildcard *.m4)))

#%.m4 : %.dockerfile
#	m4 $< > $@
# How to create a directory name from the file base name

#default: $(dockerfiles)

dockerfile:
	m4 DWSpectrum.m4 > DWSpectrum.dockerfile
	cp DWSpectrum.dockerfile ../DWSpectrum/Dockerfile

	m4 DWSpectrum-LSIO.m4 > DWSpectrum-LSIO.dockerfile
	cp DWSpectrum-LSIO.dockerfile ../DWSpectrum-LSIO/Dockerfile

	m4 NxWitness.m4 > NxWitness.dockerfile
	cp NxWitness.dockerfile ../NxWitness/Dockerfile

	m4 NxWitness-LSIO.m4 > NxWitness-LSIO.dockerfile
	cp NxWitness-LSIO.dockerfile ../NxWitness-LSIO/Dockerfile

	m4 NxMeta.m4 > NxMeta.dockerfile
	cp NxMeta.dockerfile ../NxMeta/Dockerfile

	m4 NxMeta-LSIO.m4 > NxMeta-LSIO.dockerfile
	cp NxMeta-LSIO.dockerfile ../NxMeta-LSIO/Dockerfile
