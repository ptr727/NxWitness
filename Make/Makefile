# Dependencies:
# sudo apt install make m4 docker-compose

# Test in Docker container:
# docker run -it --rm ubuntu:focal /bin/bash
# export DEBIAN_FRONTEND=noninteractive
# apt-get update
# apt-get upgrade --yes
# apt-get install --no-install-recommends --yes mc nano strace wget gdb
# wget --no-verbose --no-check-certificate --output-document=./vms_server.deb https://updates.networkoptix.com/metavms/5.0.0.33560%20R3/linux/metavms-server-5.0.0.33560-linux_x64-patch.deb
# Install:
# apt-get install --no-install-recommends --yes ./vms_server.deb
# Extract DEB package:
# dpkg-deb -R ./vms_server.deb ./vms_server
# Debug install errors:
# dpkg --debug=72200 --install ./vms_server.deb

# Debug built container, names come from Test.yml:
# docker run -it --rm test_nxwitness /bin/bash

# Delete all unused container images:
# docker image prune --all

# Buil Steps:
# make all
# ./Up.sh
# Test images by openeing the browser links
# make clean

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

.PHONY: all
all: down entrypoint create build

.PHONY: down
down:
	./Down.sh

.PHONY: up
up:
	./Up.sh

.PHONY: check
check:
	@echo "Usage:"
	@echo "'make entrypoint' : Copy entrypoint files"
	@echo "'make create' : Create Docker files"
	@echo "'make build' : Build Docker files"
	@echo "'make clean' : Delete Docker images"
	@echo "'make all' : Runs all steps"
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
clean: down $(DIRS:%=%/.docker-clean)
