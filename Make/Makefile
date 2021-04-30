# Dependencies:
# sudo apt install make m4

# Test in Docker container
# docker run -it --rm ubuntu:bionic /bin/bash

# Download and extract DEB file
# wget --no-verbose --no-check-certificate --output-document=./vms_server.deb https://updates.networkoptix.com/metavms/4.3.0.32743%20R1/linux/metavms-server-4.3.0.32743-linux_x64.deb
# dpkg-deb -R ./vms_server.deb ./vms_server

.DEFAULT_GOAL := check

lowercase = $(subst A,a,$(subst B,b,$(subst C,c,$(subst D,d,$(subst E,e,$(subst F,f,$(subst G,g,$(subst H,h,$(subst I,i,$(subst J,j,$(subst K,k,$(subst L,l,$(subst M,m,$(subst N,n,$(subst O,o,$(subst P,p,$(subst Q,q,$(subst R,r,$(subst S,s,$(subst T,t,$(subst U,u,$(subst V,v,$(subst W,w,$(subst X,x,$(subst Y,y,$(subst Z,z,$1))))))))))))))))))))))))))

M4S := $(wildcard *.m4)
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
	@echo "Usage:"
	@echo "'make lsio' : Copy LSIO files"
	@echo "'make create' : Create Docker files"
	@echo "'make build' : Build Docker files"
	@echo "'make clean' : Delete Docker images"
	@echo ""
	@echo "Variables:"
	@echo "M4S = $(M4S)"
	@echo "DIRS = $(DIRS)"
	@echo "LOWERDIRS = $(LOWERDIRS)"

.PHONY: lsio
lsio:
	cp -R ../LSIO/* ../DWSpectrum-LSIO/root
	cp -R ../LSIO/* ../NxWitness-LSIO/root
	cp -R ../LSIO/* ../NxMeta-LSIO/root

.PHONY: create
create: $(DIRS:%=%/.docker-create)

.PHONY: build
build: $(DIRS:%=%/.docker-build)

.PHONY: clean
clean:
	-rm -f $(DIRS:%=../%/.docker-build)
	-rm -f $(DIRS:%=../%/.docker-create)
	#-rm -f $(DIRS:%=../%/Dockerfile)
	-docker image rm $(LOWERDIRS)
