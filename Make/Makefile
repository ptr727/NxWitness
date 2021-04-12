# sudo apt install make m4
# docker run -it --rm ubuntu:bionic /bin/bash

.DEFAULT_GOAL := check

lowercase = $(subst A,a,$(subst B,b,$(subst C,c,$(subst D,d,$(subst E,e,$(subst F,f,$(subst G,g,$(subst H,h,$(subst I,i,$(subst J,j,$(subst K,k,$(subst L,l,$(subst M,m,$(subst N,n,$(subst O,o,$(subst P,p,$(subst Q,q,$(subst R,r,$(subst S,s,$(subst T,t,$(subst U,u,$(subst V,v,$(subst W,w,$(subst X,x,$(subst Y,y,$(subst Z,z,$1))))))))))))))))))))))))))

M4S := $(wildcard *.m4)
#DIRS := DWSpectrum DWSpectrum-LSIO NxWitness NxWitness-LSIO NxMeta NxMeta-LSIO
DIRS := $(basename $(M4S))
LOWERDIRS := $(call lowercase,$(DIRS))

%/.docker-build: %/Dockerfile
	docker build -t $(call lowercase,$(patsubst %/,%,$(basename $@))) ../$(dir $@)
	touch "../$@"

%/.docker-create: %/Dockerfile
	touch "../$@"

%/Dockerfile: %.m4
	m4 $< >../$@

.PHONY: check
check:
	@echo "M4S = $(M4S)"
	@echo "DIRS = $(DIRS)"
	@echo "LOWERDIRS = $(LOWERDIRS)"

.PHONY: build
build: $(DIRS:%=%/.docker-build)

.PHONY: create
create: $(DIRS:%=%/.docker-create)

.PHONY: clean
clean:
	-rm -f $(DIRS:%=../%/.docker-build)
	-rm -f $(DIRS:%=../%/.docker-create)
	#-rm -f $(DIRS:%=../%/Dockerfile)
	-docker image rm $(LOWERDIRS)
