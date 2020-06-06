.DEFAULT_GOAL := check

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

# Why does make report up to date even after clean?
# $(foreach file, $(OUTPUTS), $(cp $(file) $(abspath ${CURDIR}/..)/$(basename $(notdir $(file)))/Dockerfile))
replace:
	cp DWSpectrum.dockerfile ../DWSpectrum/Dockerfile
	cp DWSpectrum-LSIO.dockerfile ../DWSpectrum-LSIO/Dockerfile
	cp NxWitness.dockerfile ../NxWitness/Dockerfile
	cp NxWitness-LSIO.dockerfile ../NxWitness-LSIO/Dockerfile
	cp NxMeta.dockerfile ../NxMeta/Dockerfile
	cp NxMeta-LSIO.dockerfile ../NxMeta-LSIO/Dockerfile
