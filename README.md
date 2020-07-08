# Docker Projects for Nx Witness and Nx Meta and DW Spectrum

This is a project to build docker containers for [Network Optix Nx Witness VMS](https://www.networkoptix.com/nx-witness/), and [Network Optix Nx Meta VMS](https://meta.nxvms.com/), the developer test and preview version of Nx Witness, and [Digital Watchdog DW Spectrum IPVMS](https://digital-watchdog.com/productdetail/DW-Spectrum-IPVMS/), the US licensed and OEM branded version of Nx Witness.

## License

![GitHub License](https://img.shields.io/github/license/ptr727/NxWitness)

## Build Status

[Code](https://github.com/ptr727/NxWitness):  
![GitHub Last Commit](https://img.shields.io/github/last-commit/ptr727/nxwitness?logo=github)

[NxWitness](https://hub.docker.com/r/ptr727/nxwitness):  
![Docker Cloud Build Status](https://img.shields.io/docker/cloud/build/ptr727/nxwitness?logo=docker)

[NxWitness-LSIO](https://hub.docker.com/r/ptr727/nxwitness-lsio):  
![Docker Cloud Build Status](https://img.shields.io/docker/cloud/build/ptr727/nxwitness-lsio?logo=docker)

[NxMeta](https://hub.docker.com/r/ptr727/nxmeta):  
![Docker Cloud Build Status](https://img.shields.io/docker/cloud/build/ptr727/nxmeta?logo=docker)

[NxMeta-LSIO](https://hub.docker.com/r/ptr727/nxmeta-lsio):  
![NxMetaDocker Cloud Build Status](https://img.shields.io/docker/cloud/build/ptr727/nxmeta-lsio?logo=docker)

[DWSpectrum](https://hub.docker.com/r/ptr727/dwspectrum):  
![Docker Cloud Build Status](https://img.shields.io/docker/cloud/build/ptr727/dwspectrum?logo=docker)

[DWSpectrum-LSIO](https://hub.docker.com/r/ptr727/dwspectrum-lsio):  
![Docker Cloud Build Status](https://img.shields.io/docker/cloud/build/ptr727/dwspectrum-lsio?logo=docker)

## Overview

### Introduction

My initial inspiration to convert my DW Spectrum system running on a VM to docker came from [The Home Repot NxWitness](https://github.com/thehomerepot/nxwitness) project. I started with one GitHub docker project repository, then two, then four, and by five I decided to consolidate the various, but very similar, repositories into a single project, supporting multiple product variants.  

I try to keep my implementations in sync with the Network Optix [reference docker project](https://github.com/networkoptix/nx_open_integrations/tree/master/docker).  
The Network Optix development team is receptive to feedback, and has made several improvements in support of docker. The most recent change allowed for the removal of the complicated `systemd` related modifications.

The biggest outstanding docker challenges are hardware bound licensing, and lack of admin defined storage locations.

### Products

The project supports three product variants:

- [Network Optix Nx Witness VMS](https://www.networkoptix.com/nx-witness/).
- [Network Optix Nx Meta VMS](https://meta.nxvms.com/), the developer test and preview version of Nx Witness.
- [Digital Watchdog DW Spectrum IPVMS](https://digital-watchdog.com/productdetail/DW-Spectrum-IPVMS/), the US licensed and OEM branded version of Nx Witness.

### Base Images

The project create two variants of each product using different base images:

- [Ubuntu](https://ubuntu.com/) using [ubuntu:bionic](https://hub.docker.com/_/ubuntu) base image.
- [LinuxServer](https://www.linuxserver.io/) using [lsiobase/ubuntu:bionic](https://hub.docker.com/r/lsiobase/ubuntu) base image.

Note, I can use smaller base images like [alpine](https://hub.docker.com/_/alpine), but the mediaserver officially [supports](https://support.networkoptix.com/hc/en-us/articles/205313168-Nx-Witness-Operating-System-Support) Ubuntu Bionic.

### LinuxServer

The [LinuxServer (LSIO)](https://www.linuxserver.io/) base images provide valuable functionality:

- The LSIO images are based on [s6-overlay](https://github.com/just-containers/s6-overlay), and LSIO [produces](https://fleet.linuxserver.io/) containers for many popular open source apps.
- LSIO allows us to [specify](https://docs.linuxserver.io/general/understanding-puid-and-pgid) the user account to use when running the container process.
- This is [desired](https://docs.docker.com/develop/develop-images/dockerfile_best-practices/#user) if we do not want to run as root, or required if we need user specific permissions when accessing mapped volumes.
- We could achieve a similar outcome by using Docker's [--user](https://docs.docker.com/engine/reference/run/#user) option, but it is often more convenient to modify environment variables vs. controlling how a container runs.

### Unraid

I run [Unraid](https://unraid.net/) in my home lab, and I make sure the LSIO containers work on Unraid:

- The LSIO images work well on Unraid, because I can specify user permissions for mapped shares, and I save some space because layers are shared between several other LSIO based images I run.
- I include Unraid Docker Templates simplifying provisioning on Unraid.
- There are still Nx Witness issues when using Unraid user shares for storage, see the various notes sections.

## Configuration

The docker configuration is reasonably simple, requiring just two volume mappings for configuration files and media storage.

### Volumes

`/config` : Configuration files.  
`/media` : Recording files.  
`/archive` : Backup files. (Optional)

Note, the current Nx Witness backup implementation is [not very useful](https://support.networkoptix.com/hc/en-us/community/posts/360044221713-Backup-retention-policy), as it only makes a copy of the recordings, it does not extend the retention period.

Note, the mediaserver filters [filesystems](https://github.com/networkoptix/nx_open_integrations/tree/master/docker#notes-about-storage) by type, and the `/media` mapping must point to a supported filesytem. The upcoming version 4.1 [will support](https://support.networkoptix.com/hc/en-us/community/posts/360044241693-NxMeta-4-1-Beta-on-Docker) user defined filesystems. Unraid's FUSE filesystem is not supported, and requires the mapping of a physical device using the [Unassigned Devices](https://forums.unraid.net/topic/44104-unassigned-devices-managing-disk-drives-and-remote-shares-outside-of-the-unraid-array/) plugin.  

### Ports

`7001` : Default server port.

### Environment Variables

`PUID` : User Id (LSIO only, see [docs](https://docs.linuxserver.io/general/understanding-puid-and-pgid) for usage).  
`PGID` : Group Id (LSIO only).  
`TZ` : Timezone, e.g. `Americas/Los_Angeles`.

### Network Mode

Any network mode can be used, but due to the hardware bound licensing, `host` mode is [preferred](https://github.com/networkoptix/nx_open_integrations/tree/master/docker#networking).

## Examples

### Docker Create

```console
docker create \
  --name=dwspectrum-lsio-test-container \
  --hostname=dwspectrum-lsio-test-host \
  --domainname=foo.bar.net \
  --restart=unless-stopped \
  --network=host \
  --env TZ=Americas/Los_Angeles \
  --volume /mnt/dwspectrum/config:/config:rw \
  --volume /mnt/dwspectrum/media:/media:rw \
  ptr727/dwspectrum-lsio

docker start dwspectrum-lsio-test-container
```

### Docker Compose

```yaml
version: "3.7"

services:
  dwspectrum:
    image: ptr727/dwspectrum-lsio
    container_name: dwspectrum-lsio-test-container
    restart: unless-stopped
    network_mode: host
    environment:
      - TZ=Americas/Los_Angeles
    volumes:
      - /mnt/dwspectrum/config:/config
      - /mnt/dwspectrum/media:/media
```

### Unraid Template

- Add the template [URL](./Unraid) `https://github.com/ptr727/NxWitness/tree/master/Unraid` to the "Template Repositories" section, at the bottom of the "Docker" configuration tab, and click "Save".
- Use the [Unassigned Devices](https://forums.unraid.net/topic/44104-unassigned-devices-managing-disk-drives-and-remote-shares-outside-of-the-unraid-array/) plugin and mount a SSD drive formatted as XFS. This is currently a required workaround for the mediaserver filesystem filtering.
- Create a new container by clicking the "Add Container" button, select the desired product template from the dropdown.
- Map the Unassigned Devices SSD drive to the `/media` volume, using `RW/Slave` access mode.
- Use the `nobody` user and `users` group Id's, e.g. `PUID=99` and `PGID=100`.

## Product Releases

Product releases and updates can be found at the following locations:

- [Nx Witness Downloads](https://nxvms.com/download/linux)
- [Nx Witness Beta Downloads](https://beta.networkoptix.com/beta-builds/default/)
  - Look in the "patches" section.
- [Nx Meta Early Access Signup](https://support.networkoptix.com/hc/en-us/articles/360046713714-Get-an-Nx-Meta-Build)
- [Nx Meta Downloads](https://meta.nxvms.com/downloads/patches)
- [DW Spectrum Downloads](https://dwspectrum.digital-watchdog.com/download/linux)
  - The latest DW Spectrum versions are not listed, but can be downloaded.
  - Use the latest NX Witness URL, and substitute the "default" string for "digitalwatchdog", e.g.:
    - [http://updates.networkoptix.com/default/30917/linux/nxwitness-server-4.0.0.30917-linux64.deb](http://updates.networkoptix.com/default/30917/linux/nxwitness-server-4.0.0.30917-linux64.deb)
    - [https://updates.networkoptix.com/digitalwatchdog/30917/linux/dwspectrum-server-4.0.0.30917-linux64.deb](https://updates.networkoptix.com/digitalwatchdog/30917/linux/dwspectrum-server-4.0.0.30917-linux64.deb)

## Build Process

With three products and two base images we end up with six different dockerfiles, that all basically look the same. Unfortunately Docker does [not support](https://github.com/moby/moby/issues/735) an `include` directive, but we [can use](http://bobbynorton.com/posts/includes-in-dockerfiles-with-m4-and-make/) a `Makefile` to dynamically create a `Dockerfile` for us.

I use [Docker Hub Automated Builds](https://docs.docker.com/docker-hub/builds/) to automatically trigger, build, and publish images to [Docker Hub](https://hub.docker.com/u/ptr727). I kept the Docker Hub repositories and image names separate, but it would be possible to publish different products using different tags for one image, it just does not seem natural.

I am considering using GitHub Actions to automatically build the containers, but I've not yet figured out how to have GitHub Actions automatically rebuild the container when the upstream base image changes, this functionality is included in Docker Hub Automated Builds.

I should also figure out how to automatically detect newly released versions, and automatically rebuild when a new version is detected. This could possibly be done using page change notifiers and webhooks, with some code to extract the relevant version and URL attributes from the web pages. For now I'm manually updating as I notice the versions changing.

## Network Optix and Docker

### Issues

The biggest outstanding docker challenges are hardware bound licensing, and lack of admin defined storage locations.

The camera license keys are activated using hardware attributes of the server, that is not docker friendly, and occasionally results in activation failures. I would much prefer a modern approach to apply licenses to my cloud account, allowing me to run on whatever hardware I want. Living in the US, I have to buy my licenses from [Digital Watchdog](https://digital-watchdog.com/), and in my experience their license enforcement policy is "draconian", three activations and you have to buy a new license. That really means that the [Lifetime Upgrades and No Annual Agreements](https://digital-watchdog.com/dws/upgrades/) license is the lifetime of the hardware on which the license was activated. So let's say hardware is replaced every two years, three activations, lifetime is about six years, not much of a lifetime compared to other products that are license flexible but require maintenance or renewals.

As for storage, the mediaserver attempts to automatically decide what storage to use, applies filesystem type and instance filtering, and blindly creates files on any storage it deems fit. This overly complicated logic does not work in docker, and a much simpler and reliable approach would be to allow an admin to be in control and specify exactly what storage to be used. I really do not understand what the reasons were for building complicated decision logic, instead of being like all other servers that use storage and let the admin define the storage locations.

All in, Nx Witness is in my experience still the lightest on resources with good features VMS/NVR I've used, and with docker support, is great to run in my home lab.

### Wishlist

My wishlist for better [docker support](https://support.networkoptix.com/hc/en-us/articles/360037973573-How-to-run-Nx-Server-in-Docker):

- Publish always up to date and ready to use docker images on Docker Hub.
- Use the cloud account for license enforcement, not the hardware that dynamically changes in docker environments.
- Allow the administrator to specify and use any storage location, stop making incorrect automated storage decisions.
- Implement a [more useful](https://support.networkoptix.com/hc/en-us/community/posts/360044221713-Backup-retention-policy) recording archive management system, allowing for separate high speed recording, and high capacity playback storage volumes.

## Notes

- The CDN SSL certificates are not trusted on all systems, and we need to disable certificate checks when using HTTPS for downloads. `ERROR: cannot verify updates.networkoptix.com's certificate, issued by 'CN=Amazon,OU=Server CA 1B,O=Amazon,C=US': Unable to locally verify the issuer's authority. To connect to updates.networkoptix.com insecurely, use --no-check-certificate`

### Version 4.0.0.30917

- The mediaserver filters mapped storage volumes by filesystem type, and does not allow the admin to specify desired storage locations.
  - Look for warning messages in the logs, e.g. `QnStorageManager(0x7f863c054bd0): No storage available for recording`.
  - Because of filesystem type filtering, no mapped media storage is detected on [Unraid](https://unraid.net), [Docker Desktop for Windows](https://www.docker.com/products/docker-desktop), or [ZFS](https://zfsonlinux.org/) storage volumes.
  - Per Network Optix support, only the following filesystems are currently supported: `vfat, ecryptfs, fuseblk, fuse, fusectl, xfs, ext3, ext2, ext4, exfat, rootfs, nfs, nfs4, nfsd, cifs, fuse.osxfs`.
  - Output from `cat /proc/mounts` for a few filesystems I tested:
    - Unraid : `shfs /media fuse.shfs rw,nosuid,nodev,noatime,user_id=0,group_id=0,allow_other 0 0`
    - Docker Desktop for Windows : `grpcfuse /media fuse.grpcfuse rw,nosuid,nodev,relatime,user_id=0,group_id=0,allow_other,max_read=1048576 0 0`
    - Docker on Ubuntu Server EXT4 : `/dev/vda2 /media ext4 rw,relatime,data=ordered 0 0`
    - Docker on Proxmox ZFS : `ssdpool/dwspectrum/media /media zfs rw,noatime,xattr,posixacl 0 0`
- In Ubuntu Server, with a [non-root user](https://docs.docker.com/install/linux/linux-postinstall/), we get a runtime failure: `start-stop-daemon: unable to start /opt/digitalwatchdog/mediaserver/bin/mediaserver-bin (Invalid argument)`.
- The calculation of `VMS_DIR=$(dirname $(dirname "${BASH_SOURCE[0]}"))` in `../bin/mediaserver` results in bad paths e.g. `start-stop-daemon: unable to stat ./bin/./bin/mediaserver-bin (No such file or directory)`.
- The DEB installer does not reference all used dependencies. When trying to minimizing the size of the install by using `--no-install-recommends` we get a `OCI runtime create failed` error. We have to manually add the following required dependencies: `gdb gdbserver binutils lsb-release`.

### Version 4.1.0.31193 R8 RC

- Follow the [discussion](https://support.networkoptix.com/hc/en-us/community/posts/360044241693-NxMeta-4-1-Beta-on-Docker) in the Developer Forum.
- The recently updated, and moved from GitLab to GitHub, [Network Optix Docker project](https://github.com/networkoptix/nx_open_integrations/tree/master/docker) states that `systemd` is no longer required, and this project was modified to remove the `systemd` modifications, making ongoing maintenance much simpler.
- The upcoming version 4.1 will include the ability to specify additional storage filesystems.
  - Access the server storage page, and verify that all mounted storage is listed, e.g. `http://localhost:7001/static/index.html#/info`.
  - If storage is not listed, attach to the container and run `cat /proc/mounts` to get a list of all the filesystem types, make a note of the filesystem types that are not showing up in storage.
  - Add `fuse.grpcfuse` for Docker for Windows and `fuse.shfs` for Unraid, and `zfs` for ZFS, e.g. `fuse.grpcfuse,fuse.shfs,zfs`.
  - Save the settings, restart the server, and verify that storage is now available.
- Python was removed from the dependencies list, and `config_helper.py` was replaced with `config_helper.sh`.
- The [calculation](http://mywiki.wooledge.org/BashFAQ/028) of `VMS_DIR=$(dirname $(dirname "${BASH_SOURCE[0]}"))` in `../bin/mediaserver` can result in bad paths when called from the same directory, e.g. `start-stop-daemon: unable to stat ./bin/./bin/mediaserver-bin (No such file or directory)`.
- The filesystem filter logic incorrectly considers some volumes to be duplicates, turn on verbose logging : `2020-05-18 10:13:55.964    422 VERBOSE nx::vms::server::fs: shfs /archive fuse.shfs - duplicate`.
- There is no apparent way to configure the `additionalLocalFsTypes` types at deployment time, it can only be done post deployment from the `http://localhost:7001/static/index.html#/advanced` web interface or via `http://admin:<passsword>@localhost:7001/api/systemSettings?additionalLocalFsTypes=fuse.grpcfuse,fuse.shfs`.
  - Some debugging shows the setting is stored in the `var/ecs.sqlite` DB file, in the `vms_kvpair` table, `name=additionalLocalFsTypes`, `value=fuse.grpcfuse,fuse.shfs,zfs`.
  - This DB table contains lots of other information, so it seems unfeasible to pre-seed the system with this DB file, and modifying it at runtime is as complex as calling the web service.
- The mediaserver pollutes the filesystem by blindly creating a `Nx MetaVMS Media` folder and DB files in any storage it finds.
