# Docker Projects for Nx Witness and Nx Meta and DW Spectrum

This is a project to build docker containers for [Network Optix Nx Witness VMS](https://www.networkoptix.com/nx-witness/), and [Network Optix Nx Meta VMS](https://meta.nxvms.com/), the developer test and preview version of Nx Witness, and [Digital Watchdog DW Spectrum IPVMS](https://digital-watchdog.com/productdetail/DW-Spectrum-IPVMS/), the US licensed and OEM branded version of Nx Witness.

## License

![GitHub License](https://img.shields.io/github/license/ptr727/NxWitness)

## Build Status

[Code and Pipeline is on GitHub](https://github.com/ptr727/NxWitness):  
![GitHub Last Commit](https://img.shields.io/github/last-commit/ptr727/NxWitness?logo=github)  
![GitHub Workflow Status](https://img.shields.io/github/workflow/status/ptr727/NxWitness/Build%20and%20Publish%20Docker%20Images?logo=github)

## Container Images

Docker container images are published on [Docker Hub](https://hub.docker.com/u/ptr727) and [GitHub Container Registry](https://github.com/ptr727?tab=packages&repo_name=NxWitness).  
Images are tagged using `latest` or `stable` and the specific build version number.  
`latest` images use the latest patch release version.  
`stable` images use the last stable release version.  
This allows flexibility for deployments that want to pin to specific release channels or versions.  
E.g.

```console
docker pull docker.io/ptr727/nxmeta-lsio:latest
docker pull ghcr.io/ptr727/dwspectrum:stable
docker pull docker.io/ptr727/nxwitness-lsio:5.0.0.35136
```

The images are updated weekly, picking up the latest upstream OS updates, and stable build version updates.

[NxWitness](https://hub.docker.com/r/ptr727/nxwitness)  
![Docker Image Version](https://img.shields.io/docker/v/ptr727/nxwitness/latest?label=latest&logo=docker)
![Docker Image Version](https://img.shields.io/docker/v/ptr727/nxwitness/stable?label=stable&logo=docker)

[NxWitness-LSIO](https://hub.docker.com/r/ptr727/nxwitness-lsio)  
![Docker Image Version](https://img.shields.io/docker/v/ptr727/nxwitness-lsio/latest?label=latest&logo=docker)
![Docker Image Version](https://img.shields.io/docker/v/ptr727/nxwitness-lsio/stable?label=stable&logo=docker)

[NxMeta](https://hub.docker.com/r/ptr727/nxmeta)  
![Docker Image Version](https://img.shields.io/docker/v/ptr727/nxmeta/latest?label=latest&logo=docker)
![Docker Image Version](https://img.shields.io/docker/v/ptr727/nxmeta/stable?label=stable&logo=docker)

[NxMeta-LSIO](https://hub.docker.com/r/ptr727/nxmeta-lsio)  
![Docker Image Version](https://img.shields.io/docker/v/ptr727/nxmeta-lsio/latest?label=latest&logo=docker)
![Docker Image Version](https://img.shields.io/docker/v/ptr727/nxmeta-lsio/stable?label=stable&logo=docker)

[DWSpectrum](https://hub.docker.com/r/ptr727/dwspectrum)  
![Docker Image Version](https://img.shields.io/docker/v/ptr727/dwspectrum/latest?label=latest&logo=docker)
![Docker Image Version](https://img.shields.io/docker/v/ptr727/dwspectrum/stable?label=stable&logo=docker)

[DWSpectrum-LSIO](https://hub.docker.com/r/ptr727/dwspectrum-lsio)  
![Docker Image Version](https://img.shields.io/docker/v/ptr727/dwspectrum-lsio/latest?label=latest&logo=docker)
![Docker Image Version](https://img.shields.io/docker/v/ptr727/dwspectrum-lsio/stable?label=stable&logo=docker)

## Overview

### Introduction

I ran DW Spectrum in my home lab on an Ubuntu Virtual Machine, and was looking for a way to run it in Docker. Nx Witness provided no support for Docker, but I did find the [The Home Repot NxWitness](https://github.com/thehomerepot/nxwitness) project, that inspired me to create this project.  
I started with individual repositories for Nx Witness, Nx Meta, and DW Spectrum, but that soon became cumbersome with lots of duplication, and I combined all product flavors into this one project.

As of recent Network Optix does provide [Experimental Docker Support](https://support.networkoptix.com/hc/en-us/articles/360037973573-How-to-run-Nx-Server-in-Docker), and they publish a [reference docker project](https://github.com/networkoptix/nx_open_integrations/tree/master/docker), but they do not publish container images.  

The biggest outstanding challenges with running in docker are hardware bound licensing and lack of admin defined storage locations, see the [Network Optix and Docker](#network-optix-and-docker) section for details.  

### Products

The project supports three product variants:

- [Network Optix Nx Witness VMS](https://www.networkoptix.com/nx-witness/).
- [Network Optix Nx Meta VMS](https://meta.nxvms.com/), the developer test and preview version of Nx Witness.
- [Digital Watchdog DW Spectrum IPVMS](https://digital-watchdog.com/productdetail/DW-Spectrum-IPVMS/), the US licensed and OEM branded version of Nx Witness.

### Base Images

The project creates two variants of each product using different base images:

- [Ubuntu](https://ubuntu.com/) using [ubuntu:focal](https://hub.docker.com/_/ubuntu) base image.
- [LinuxServer](https://www.linuxserver.io/) using [lsiobase/ubuntu:focal](https://hub.docker.com/r/lsiobase/ubuntu) base image.

Note that smaller base images, like [Alpine](https://alpinelinux.org/), and the current [Ubuntu 22.04 LTS (Jammy Jellyfish)](https://releases.ubuntu.com/22.04/) are not [supported](https://support.networkoptix.com/hc/en-us/articles/205313168-Nx-Witness-Operating-System-Support) by the mediaserver.

### LinuxServer

The [LinuxServer (LSIO)](https://www.linuxserver.io/) base images provide valuable functionality:

- The LSIO images are based on [s6-overlay](https://github.com/just-containers/s6-overlay), and LSIO [produces](https://fleet.linuxserver.io/) containers for many popular open source applications.
- LSIO allows us to [specify](https://docs.linuxserver.io/general/understanding-puid-and-pgid) the user account to use when running the container mediaserver process.
- This is [desired](https://docs.docker.com/develop/develop-images/dockerfile_best-practices/#user) if we do not want to run as root, or required if we need user specific permissions when accessing mapped volumes.
- We could achieve a similar outcome by using Docker's [`--user`](https://docs.docker.com/engine/reference/run/#user) option, but the mediaserver's `root-tool` (used for license enforcement) requires running as `root`, thus the container must still be executed with `root` privileges, and we cannot use the `--user` option.
- The non-LSIO images do run the mediaserver as a non-root user, granting `sudo` rights to run the `root-tool` as `root`, but the user account `${COMPANY_NAME}` does not readily map to a user on the host system.

## Configuration

The docker configuration is simple, requiring just two volume mappings for configuration files and media storage.

### Volumes

`/config` : Configuration files.  
`/media` : Recording files.  
`/archive` : Backup files. (Optional)

Note that if your storage is not showing up, see the [Missing Storage](#missing-storage) section for help.

### Ports

`7001` : Default server port.

### Environment Variables

`PUID` : User Id (LSIO only, see [docs](https://docs.linuxserver.io/general/understanding-puid-and-pgid) for usage).  
`PGID` : Group Id (LSIO only).  
`TZ` : Timezone, e.g. `Americas/Los_Angeles`.

### Network Mode

Any network mode can be used, but due to the hardware bound licensing, `host` mode is [recommended](https://github.com/networkoptix/nx_open_integrations/tree/master/docker#networking).

## Examples

### Docker Create

```console
docker create \
  --name=nxwitness-lsio-test-container \
  --hostname=nxwitness-lsio-test-host \
  --domainname=foo.bar.net \
  --restart=unless-stopped \
  --network=host \
  --env TZ=Americas/Los_Angeles \
  --volume /mnt/nxwitness/config:/config:rw \
  --volume /mnt/nxwitness/media:/media:rw \
  ptr727/nxwitness-lsio:stable

docker start nxwitness-lsio-test-container
```

### Docker Compose

```yaml
version: "3.7"

services:
  nxwitness:
    image: ptr727/nxwitness-lsio:stable
    container_name: nxwitness-lsio-test-container
    restart: unless-stopped
    network_mode: host
    environment:
      - TZ=Americas/Los_Angeles
    volumes:
      - /mnt/nxwitness/config:/config
      - /mnt/nxwitness/media:/media
```

### Non-LSIO Docker Compose

The LSIO images re-link internal paths, while the non-LSIO images needs to map volumes directly to the installed folders.

```yaml
version: "3.7"

services:
  nxwitness:
    image: ptr727/nxwitness:stable
    container_name: nxwitness-test-container
    restart: unless-stopped
    network_mode: host
    volumes:
      - /mnt/nxwitness/config/etc:/opt/networkoptix/mediaserver/etc
      - /mnt/nxwitness/media:/opt/networkoptix/mediaserver/var/
```

### Unraid Template

- Add the template [URL](./Unraid) `https://github.com/ptr727/NxWitness/tree/master/Unraid` to the "Template Repositories" section, at the bottom of the "Docker" configuration tab, and click "Save".
- Create a new container by clicking the "Add Container" button, select the desired product template from the dropdown.
- If using Unassigned Devices for media storage, use `RW/Slave` access mode.
- Use `nobody` and `users` identifiers, `PUID=99` and `PGID=100`.
- Register the Unraid filesystems in the `additionalLocalFsTypes` advanced settings, see the [Missing Storage](#missing-storage) section for help.

## Release Tracking

Product releases, updates, and information can be found at the following locations:

- Nx Witness:
  - [JSON API](https://nxvms.com/api/utils/downloads)
  - [Downloads](https://nxvms.com/download/linux)
  - [Beta Downloads](https://beta.networkoptix.com/beta-builds/default)
  - [Release Notes](https://www.networkoptix.com/all-nx-witness-release-notes)
- Nx Meta:
  - [JSON API](https://meta.nxvms.com/api/utils/downloads)
  - [Early Access Signup](https://support.networkoptix.com/hc/en-us/articles/360046713714-Get-an-Nx-Meta-Build)
  - [Request Developer Licenses](https://support.networkoptix.com/hc/en-us/articles/360045718294-Getting-Licenses-for-Developers)
  - [Downloads](https://meta.nxvms.com/download/linux)
  - [Beta Downloads](https://meta.nxvms.com/downloads/patches)
- DW Spectrum:
  - [JSON API](https://dwspectrum.digital-watchdog.com/api/utils/downloads)
  - [Downloads](https://dwspectrum.digital-watchdog.com/download/linux)
  - [Release Notes](https://digital-watchdog.com/DWSpectrum-Releasenote/DWSpectrum.html)
  - Note that DW Spectrum versions often lag Nx Witness releases.

Updating the mediaserver inside docker is not supported, to update the server version pull a new container image, it is "the docker way".

## Build Process

With three products and two base images we end up with six different dockerfiles, that all basically look the same. Unfortunately Docker does [not support](https://github.com/moby/moby/issues/735) a native `include` directive, so I use the [M4 macro processor](https://www.gnu.org/software/m4/) and a `Makefile` to dynamically create a `Dockerfile` for every variant.

Updating the product versions and download URL's are done using the custom `CreateMatrix`  utility app.  
The app takes a [Version.json](./Make/Version.json) file as input, creates permutations for products, base images, latest versions, stable versions, develop builds, and produces a [Matrix.json](./Make/Matrix.json) file as output.  
All the images are built using GitHub Actions using a [Matrix](https://docs.github.com/en/actions/using-jobs/using-a-matrix-for-your-jobs) strategy derived from the `Matrix.json` file.

The `CreateMatrix` utility will optionally query the latest `stable` product versions using the Network Optix online JSON API, and automatically build with the latest stable versions. In case the `stable` version is greater than the `latest` version, the `latest` version will be updated to match the `stable` version.

Pre-release or Beta versions are not publish via the JSON API and are thus not automatically discoverable. I do monitor the NxMeta downloads page using [VisualPing](https://visualping.io/) but it is not always reliable. In case of new versions that are not yet building, feel free to create an issue with pointers to the downloads.

## Network Optix and Docker

There are annoying and serious issues, but compared to other VMS/NVR software I've paid for and used, Nx Witness is the lightest on system resources, has a good feature set, and with added docker support runs great in my home lab.

### Licensing

**Issue:**  
The camera recording license keys are activated and bound to hardware attributes of the host server.  
Docker containers are supposed to be portable, and moving containers between hosts will break license activation.

**Possible Solution:**  
A portable approach could apply licenses to the [Cloud Account](https://www.networkoptix.com/nx-witness/nx-witness-cloud/), allowing runtime enforcement that is not hardware bound.

### Storage Management

**Issue:**  
The mediaserver attempts to automatically decide what storage to use.  
Filesystem types are filtered out if not on the [supported list](https://github.com/networkoptix/nx_open_integrations/tree/master/docker#notes-about-storage), e.g. popular and common ZFS is not supported.  
Duplicate filesystems are ignored, e.g. multiple logical mounts on the same physical storage are ignored.  
The server blindly creates database files on any writable storage it discovers, regardless of if that storage was assigned for use or not.

**Possible Solution:**  
Remove the elaborate and prone to failure filesystem discovery and filtering logic, use the specified storage, and only the specified storage.

### Network Binding

**Issue:**  
The mediaserver binds to any discovered network adapter.
On docker this means the server binds to all docker networks of all running containers, there could be hundreds or thousands, making the network graph useless, and consuming unnecessary resources.

**Possible Solution:**  
Remove the auto-bind functionality, or make it configurable with the default disabled, and allow the administrator to define the specific networks to bind with.

### Lifetime Upgrades

**Issue:**  
This section is personal opinion, I've worked in the ISV industry for many years, and I've taken perpetually licensed products to SaaS.

Living in the US, I have to buy my licenses from [Digital Watchdog](https://digital-watchdog.com/), and in my experience their license enforcement policy is inflexible, three activations and you have to buy a new license.  
That really means that the [Lifetime Upgrades and No Annual Agreements](https://dwspectrum.com/upgrades/) license is the lifetime of the hardware on which the license was activated. So let's say hardware is replaced every two years, three activations, lifetime is about six years, not much of a lifetime compared to my lifetime.  

There is no such thing as free of cost software, at minimum somebody pays for time, and at minimum vulnerabilities must be fixed, the EULA does not excuse an ISV from willful neglect.  
Add in ongoing costs of cloud hosting, cost of development of new features, and providing support, where does the money come from?  
Will we eventually see a license scheme change, or is it a customer [acquisition](https://www.crunchbase.com/organization/network-optix) and sell or go public play, but hopefully not a cash out and bail scheme?

**Possible Solution:**  
I'd be happy to pay a reasonable yearly subscription or maintenance fee, knowing I get ongoing fixes, features, and support, and my licenses being tied to my cloud account.

### Wishlist

My wishlist for better docker support:

- Publish always up to date and ready to use docker images on Docker Hub or GitHub Container Registry.
- Do not bind the license to hardware, use the cloud account for license enforcement.
- Do not filter storage filesystems, allow the administrator to specify and use any storage location.
- Do not pollute the filesystem by blindly creating folders in any detected storage.
- Do not bind to any discovered network adapter, allow the administrator to specify the bound network adapter, or add an option to opt-out/opt-in to auto-binding.
- Implement a [more useful](https://support.networkoptix.com/hc/en-us/community/posts/360044221713-Backup-retention-policy) recording archive management system, allowing for separate high speed recording, and high capacity playback storage volumes. E.g. as implemented by [Milestone XProtect VMS](https://doc.milestonesys.com/latest/en-US/standard_features/sf_mc/sf_systemoverview/mc_storageandarchivingexplained.htm).

Please do [contact](https://support.networkoptix.com/hc/en-us/community/topics) Network Optix and ask for better [docker support](https://support.networkoptix.com/hc/en-us/articles/360037973573-How-to-run-Nx-Server-in-Docker).

## Troubleshooting

I am not affiliated with Network Optix, I cannot provide support for their products, please contact [Network Optix Support](https://support.networkoptix.com/hc/en-us/community/topics) for product support issues.  
If there are issues with the docker build scripts used in this project, please create a [GitHub Issue](https://github.com/ptr727/NxWitness/issues).  
Note that I only test and run `nxmeta-lsio:stable` in my home lab, other images get very little to no testing, please test accordingly.

### Known Issues

- Windows Subsystem for Linux v2 (WSL2) is not supported.
  - The DEB installer `postinst` step the installer tries to start the service, and fails the install.
    - `Detected runtime type: wsl.`
    - `System has not been booted with systemd as init system (PID 1). Can't operate.`
  - The logic tests for `if [[ $RUNTIME != "docker" ]]`, while the runtime reported by WSL2 is `wsl` not `docker`.
  - The installer logic [should](https://support.networkoptix.com/hc/en-us/community/posts/1500000699041-WSL2-docker-runtime-not-supported) perform a `systemd` positive test vs. testing for not docker.
- The download CDN SSL certificates are not trusted on all systems.
  - `ERROR: cannot verify updates.networkoptix.com's certificate, issued by 'CN=Amazon,OU=Server CA 1B,O=Amazon,C=US': Unable to locally verify the issuer's authority. To connect to updates.networkoptix.com insecurely, use --no-check-certificate`
  - When downloading use `wget --no-verbose --no-check-certificate` to ignore the SSL error.
- Upgrading from v4.x to v5.0:
  - The old shell script `mediaserver` is now what used to be `mediaserver-bin`, and `root-tool` is now what used to be `root-tool-bin`.
  - After upgrading to 5.0, reverting to 4.2 is no longer possible.
    - `ERROR ec2::detail::QnDbManager(...): DB Error at ec2::ErrorCode ec2::detail::QnDbManager::doQueryNoLock(...): No query Unable to fetch row`
  - Make a copy of the server configuration before upgrading, and restore the old configuration when downgrading.

### Missing Storage

The following section will help troubleshoot common problems with missing storage.  
If this does not help, please contact [Network Optix Support](https://support.networkoptix.com/hc/en-us/community/topics).  
Please do not open a GitHub issue unless you are positive the issue is with the `Dockerfile`.

Verify the available storage locations:  
Open the [web admin](https://support.networkoptix.com/hc/en-us/articles/115012831028-Nx-Server-Web-Admin) interface, Right-Click on the server name in the Desktop Client, and select "Server Web Page".  
Click on "Information" and then "Storage Locations", or visit `https://[hostname]:7001/static/health.html#/health/storages`, and verify that all mounted storage is listed.

Enable debug logging in the mediaserver:  
Edit `/config/etc/mediaserver.conf`, set `logLevel=DEBUG2`, restart the server.  
Look for clues in `/config/var/log/log_file.log`.

E.g.

```log
VERBOSE nx::vms::server::fs: shfs /media fuse.shfs - duplicate
VERBOSE nx::vms::server::fs: /dev/sdb8 /media btrfs - duplicate
DEBUG QnStorageSpaceRestHandler(0x7f85043b0b00): Return 0 storages and 1 protocols
```

Get a list of the mapped volume mounts in the running container, and verify that `/config` and `/media` is in the JSON `Mounts` section:

```console
docker ps --no-trunc
docker container inspect [containername]
```

Launch a shell in the running container and get a list of filesystems mounts:

```console
docker ps --no-trunc
docker exec --interactive --tty [containername] /bin/bash
cat /proc/mounts
exit
```

Example output for ZFS:

```console
ssdpool/appdata /config zfs rw,noatime,xattr,posixacl 0 0
nvrpool/nvr /media zfs rw,noatime,xattr,posixacl 0 0
ssdpool/docker /archive zfs rw,noatime,xattr,posixacl 0 0
```

Mount `/config` is on device `ssdpool/appdata` and filesystem is `zfs`.  
Mount `/media` is on device `nvrpool/nvr` and filesystem is `zfs`.  
Mount `/archive` is on device `ssdpool/docker` and filesystem is `zfs`.

In this case the devices are unique and will not be filtered, but `zfs` is not supported and needs to be registered.

Example output for UnRaid FUSE:

```console
shfs /config fuse.shfs rw,nosuid,nodev,noatime,user_id=0,group_id=0,allow_other 0 0
shfs /media fuse.shfs rw,nosuid,nodev,noatime,user_id=0,group_id=0,allow_other 0 0
shfs /archive fuse.shfs rw,nosuid,nodev,noatime,user_id=0,group_id=0,allow_other 0 0
```

In this case there are two issues, the device is `/shfs` for all three mounts and will be filtered, and the filesystem type is `fuse.shfs` that is not supported and needs to be registered.

Log file output for Unraid FUSE:

```log
VERBOSE nx::vms::server::fs: shfs /config fuse.shfs - added
VERBOSE nx::vms::server::fs: shfs /media fuse.shfs - added
VERBOSE nx::vms::server::fs: shfs /archive fuse.shfs - duplicate
```

The `/archive` mount is classified as a duplicate and ignored, map just `/media`, do not map `/archive`.  
Alternative use the "Unassigned Devices" plugin and dedicate e.g. a XFS formatted SSD drive to `/media` and/or `/config`.

Example output for Unraid BTRFS:

```console
/dev/sdb8 /test btrfs rw,relatime,space_cache,subvolid=5,subvol=/test 0 0
/dev/sdb8 /config btrfs rw,relatime,space_cache,subvolid=5,subvol=/config 0 0
/dev/sdb8 /media btrfs rw,relatime,space_cache,subvolid=5,subvol=/media 0 0
/dev/sdb8 /archive btrfs rw,relatime,space_cache,subvolid=5,subvol=/archive 0 0
```

```log
VERBOSE nx::vms::server::fs: /dev/sdb8 /test btrfs - added
VERBOSE nx::vms::server::fs: /dev/sdb8 /config btrfs - duplicate
VERBOSE nx::vms::server::fs: /dev/sdb8 /media btrfs - duplicate
VERBOSE nx::vms::server::fs: /dev/sdb8 /archive btrfs - duplicate
```

In this example the `/test` volume was accepted, but all other volumes on `/dev/sdb8` was ignored as duplicates.

Add the required filesystem types in the advanced configuration menu, and restart the server:  
Open the advanced configuration web admin page at `https://[hostname]:7001/static/index.html#/advanced`.  
Edit the `additionalLocalFsTypes` option and add the required filesystem types, e.g. `fuse.shfs,btrfs,zfs`.

Alternatively call the configuration API directly:  
`wget --no-check-certificate --user=[username] --password=[password] https://[hostname]:7001/api/systemSettings?additionalLocalFsTypes=fuse.shfs,btrfs,zfs`.

To my knowledge there is no solution to duplicate devices being filtered, please contact [Network Optix Support](https://support.networkoptix.com/hc/en-us/community/topics) and ask them to stop filtering filesystem types and devices.
