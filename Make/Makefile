# Dependencies:
# sudo apt install make m4

# Test in Docker container
# docker run -it --rm ubuntu:focal /bin/bash
# export DEBIAN_FRONTEND=noninteractive
# apt-get update
# apt-get upgrade --yes
# apt-get install --no-install-recommends --yes mc nano strace wget gdb
# wget --no-verbose --no-check-certificate --output-document=./vms_server.deb https://updates.networkoptix.com/metavms/4.3.0.32743%20R1/linux/metavms-server-4.3.0.32743-linux_x64.deb
# apt-get install --no-install-recommends --yes ./vms_server.deb

# Extract DEB installer:
# dpkg-deb -R ./vms_server.deb ./vms_server

# Debug install errors
# dpkg --debug=72200 --install ./vms_server.deb

# Delete all unused images
# docker image prune --all

# Buil Steps:
# ./Down.sh
# make clean
# make entrypoint
# make create
# make build
# ./Up.sh
# Test images
# ./Down.sh
# make clean

# Debug built container:
# docker run -it --rm test_nxwitness /bin/bash

.DEFAULT_GOAL := check

lowercase = $(subst A,a,$(subst B,b,$(subst C,c,$(subst D,d,$(subst E,e,$(subst F,f,$(subst G,g,$(subst H,h,$(subst I,i,$(subst J,j,$(subst K,k,$(subst L,l,$(subst M,m,$(subst N,n,$(subst O,o,$(subst P,p,$(subst Q,q,$(subst R,r,$(subst S,s,$(subst T,t,$(subst U,u,$(subst V,v,$(subst W,w,$(subst X,x,$(subst Y,y,$(subst Z,z,$1))))))))))))))))))))))))))

M4S := $(wildcard *.m4)
DIRS := $(basename $(M4S))
LOWERDIRS := $(call lowercase,$(DIRS))

%/.docker-build: %/Dockerfile
	docker build -t test_$(call lowercase,$(patsubst %/,%,$(basename $@))) ../$(dir $@)
	touch "../$@"

%/.docker-clean: %/Dockerfile
	-docker image rm test_$(call lowercase,$(patsubst %/,%,$(basename $@)))

%/.docker-create: %/Dockerfile
	touch "../$@"

%/Dockerfile: %.m4
	-rm ../$@
	m4 $< >../$@

.PHONY: check
check:
	@echo "Usage:"
	@echo "'make entrypoint' : Copy entrypoint files"
	@echo "'make create' : Create Docker files"
	@echo "'make build' : Build Docker files"
	@echo "'make clean' : Delete Docker images"
	@echo ""
	@echo "Variables:"
	@echo "M4S = $(M4S)"
	@echo "DIRS = $(DIRS)"
	@echo "LOWERDIRS = $(LOWERDIRS)"

.PHONY: entrypoint
entrypoint:
	-rm -rf ../DWSpectrum-LSIO/root
	-mkdir ../DWSpectrum-LSIO/root
	cp -R ../LSIO/* ../DWSpectrum-LSIO/root
	-rm -rf ../NxWitness-LSIO/root
	-mkdir ../NxWitness-LSIO/root
	cp -R ../LSIO/* ../NxWitness-LSIO/root
	-rm -rf ../NxMeta-LSIO/root
	-mkdir ../NxMeta-LSIO/root
	cp -R ../LSIO/* ../NxMeta-LSIO/root
	-rm -rf ../DWSpectrum/root
	-mkdir ../DWSpectrum/root
	cp -R ../Entrypoint/* ../DWSpectrum/root/
	-rm -rf ../NxWitness/root
	-mkdir ../NxWitness/root
	cp -R ../Entrypoint/* ../NxWitness/root/
	-rm -rf ../NxMeta/root
	-mkdir ../NxMeta/root
	cp -R ../Entrypoint/* ../NxMeta/root/

.PHONY: create
create: $(DIRS:%=%/.docker-create)

.PHONY: build
build: $(DIRS:%=%/.docker-build)

.PHONY: clean
clean: $(DIRS:%=%/.docker-clean)
