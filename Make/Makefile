# Dependencies:
# sudo apt install make m4 docker-compose-plugin docker-buildx-plugin

# Test installing in container:
# docker run -it --rm ubuntu:jammy /bin/bash
# docker run -it --rm lsiobase/ubuntu:jammy /bin/bash
# export DEBIAN_FRONTEND=noninteractive
# apt-get update && apt-get upgrade --yes
# apt-get install --no-install-recommends --yes mc nano strace wget gdb
# wget --no-verbose --output-document=./vms_server.zip https://updates.networkoptix.com/metavms/37996/metavms-server_update-5.1.2.37996-linux_x64.zip
# unzip -d ./download_zip ./vms_server.zip
# cp ./download_zip/metavms-server-5.1.2.37996-linux_x64.deb ./vms_server.deb
# Install:
# apt-get install --no-install-recommends --yes ./vms_server.deb
# Extract DEB package:
# dpkg-deb -R ./vms_server.deb ./vms_server
# Debug install errors:
# dpkg --debug=72200 --install ./vms_server.deb

# Run shell in container, names come from Test.yml:
# docker run -it --rm test_nxwitness /bin/bash

# Docker cleanup:
# df
# docker image prune --all
# docker system prune --all --force --volumes

# Create Dockerfiles:
# make create

# Build images:
# make build

# Launch images:
# make up
## Test images by ctrl-click opening the browser links
## Attach to running image using docker exec --interactive --tty [containername] /bin/bash

# Cleanup
# make clean

SHELL := /bin/bash

.DEFAULT_GOAL := check

lowercase = $(subst A,a,$(subst B,b,$(subst C,c,$(subst D,d,$(subst E,e,$(subst F,f,$(subst G,g,$(subst H,h,$(subst I,i,$(subst J,j,$(subst K,k,$(subst L,l,$(subst M,m,$(subst N,n,$(subst O,o,$(subst P,p,$(subst Q,q,$(subst R,r,$(subst S,s,$(subst T,t,$(subst U,u,$(subst V,v,$(subst W,w,$(subst X,x,$(subst Y,y,$(subst Z,z,$1))))))))))))))))))))))))))

M4S := $(wildcard *.m4)
DIRS := $(basename $(M4S))
LOWERDIRS := $(call lowercase,$(DIRS))

%/.docker-build: %/Dockerfile
	docker buildx build --platform linux/amd64,linux/arm64 --tag test_$(call lowercase,$(patsubst %/,%,$(basename $@))) ../$(dir $@)
	docker buildx build --platform linux/amd64 --load --tag test_$(call lowercase,$(patsubst %/,%,$(basename $@))) ../$(dir $@)
	touch "../$@"

%/.docker-clean: %/Dockerfile
	-docker image rm test_$(call lowercase,$(patsubst %/,%,$(basename $@)))

%/.docker-create: %/Dockerfile
	touch "../$@"

%/Dockerfile: %.m4
	-rm ../$@
	m4 $< >../$@

.PHONY: all
all: create build

.PHONY: down
down:
	./Down.sh

.PHONY: up
up:
	./Up.sh

.PHONY: check
check:
	@echo "Usage:"
	@echo "'make create' : Create Docker files"
	@echo "'make build' : Build Docker files"
	@echo "'make clean' : Delete Docker images"
	@echo "'make all' : Runs create and build"
	@echo ""
	@echo "Variables:"
	@echo "M4S = $(M4S)"
	@echo "DIRS = $(DIRS)"
	@echo "LOWERDIRS = $(LOWERDIRS)"

.PHONY: copy
copy:
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
	cp Download.sh ../DWSpectrum-LSIO/Download.sh
	cp Download.sh ../DWSpectrum/Download.sh
	cp Download.sh ../NxWitness-LSIO/Download.sh
	cp Download.sh ../NxWitness/Download.sh
	cp Download.sh ../NxMeta-LSIO/Download.sh
	cp Download.sh ../NxMeta/Download.sh

.PHONY: update
update:
	dotnet run --project ../CreateMatrix -- matrix --update
	dotnet run --project ../CreateMatrix -- make

.PHONY: create
create: update copy $(DIRS:%=%/.docker-create)

.PHONY: multiplat
multiplat: 
	-docker buildx create --name multiplat --use

.PHONY: build
build: down multiplat $(DIRS:%=%/.docker-build)

.PHONY: clean
clean: down $(DIRS:%=%/.docker-clean)
