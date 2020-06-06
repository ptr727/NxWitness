SRCS := $(wildcard *.m4)
DOCS := $(SRCS:%.m4=%)

all: ${DOCS}

%: %.m4
	m4 $<

dockerfile: *.m4
	m4 DWSpectrum.m4 > ../DWSpectrum/Dockerfile

clean:
	rm ../DWSpectrum/Dockerfile
	rm ../DWSpectrum-LSIO/Dockerfile
	rm ../NxMeta/Dockerfile
	rm ../NxMeta-LSIO/Dockerfile
	rm ../NxWitness/Dockerfile
	rm ../NxWitness-LSIO/Dockerfile

build: dockerfile
