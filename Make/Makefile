.DEFAULT_GOAL := check

# Even if the m4 files changed make still reports up to date?
INPUTS = $(wildcard ${CURDIR}/*.m4)
OUTPUTS = $(addsuffix .dockerfile, $(basename $(INPUTS)))
# Why does the output look funky?
TARGETS = $(abspath ${CURDIR}/..)/$(basename $(notdir $(INPUTS)))/Dockerfile

%.dockerfile : %.m4
	m4 $< > $@

check:
	@echo "INPUTS = $(INPUTS)"
	@echo "OUTPUTS = $(OUTPUTS)"
	@echo "TARGETS = $(TARGETS)"

create: $(OUTPUTS)

clean:
	-rm *.dockerfile $(OUTPUTS)
	-docker image rm dwspectrum
	-docker image rm dwspectrum-lsio
	-docker image rm nxwitness
	-docker image rm nxwitness-lsio
	-docker image rm nxmeta
	-docker image rm nxmeta-lsio

# Why does make report up to date even after clean?
# $(foreach file, $(OUTPUTS), $(cp $(file) $(abspath ${CURDIR}/..)/$(basename $(notdir $(file)))/Dockerfile))
replace:
	cp DWSpectrum.dockerfile ../DWSpectrum/Dockerfile
	cp DWSpectrum-LSIO.dockerfile ../DWSpectrum-LSIO/Dockerfile
	cp -avrT ../LSIO ../DWSpectrum-LSIO/root
	cp NxWitness.dockerfile ../NxWitness/Dockerfile
	cp NxWitness-LSIO.dockerfile ../NxWitness-LSIO/Dockerfile
	cp -avrT ../LSIO ../NxWitness-LSIO/root
	cp NxMeta.dockerfile ../NxMeta/Dockerfile
	cp NxMeta-LSIO.dockerfile ../NxMeta-LSIO/Dockerfile
	cp -avrT ../LSIO ../NxMeta-LSIO/root

build:
	docker build --pull --rm -f ../DWSpectrum/Dockerfile -t dwspectrum:latest ../DWSpectrum
	docker build --pull --rm -f ../DWSpectrum-LSIO/Dockerfile -t dwspectrum-lsio:latest ../DWSpectrum-LSIO
	docker build --pull --rm -f ../NxWitness/Dockerfile -t nxwitness:latest ../NxWitness
	docker build --pull --rm -f ../NxWitness-LSIO/Dockerfile -t nxwitness-lsio:latest ../NxWitness-LSIO
	docker build --pull --rm -f ../NxMeta/Dockerfile -t nxmeta:latest ../NxMeta
	docker build --pull --rm -f ../NxMeta-LSIO/Dockerfile -t nxmeta-lsio:latest ../NxMeta-LSIO
