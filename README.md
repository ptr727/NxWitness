# Docker Projects for Nx Witness and Nx Meta and DW Spectrum

This is a project to build docker containers for [Network Optix Nx Witness VMS][nxwitness], and [Network Optix Nx Meta VMS][nxmeta], the developer test and preview version of Nx Witness, and [Digital Watchdog DW Spectrum IPVMS][dwwatchdog], the US licensed and OEM branded version of Nx Witness.

## License

Licensed under the [MIT License][license].  
![License Shield][license_shield]

## Build Status

[![Last Commit][last_commit_shield]][repo]  
[![Workflow Status][workflow_status_shield]][actions]  
[![Last Build][last_build_shield]][actions]

## Release Notes

- Version 2.3:
  - Added unit test project, testing release and upgrade control logic.
- Version 2.2:
  - Simplified `Dockerfile` creation by using shell scripts instead of a `Makefile` (that I found too difficult to maintain and debug).
- Version 2.1:
  - Added ARM64 images per user [request](https://github.com/ptr727/NxWitness/issues/131).
    - Note that testing was limited to verifying that the containers run on a Raspberry Pi 5.
  - Updated build scripts to use `docker compose` (vs. `docker-compose`) and `docker buildx` (vs. `docker build`) per current  Docker/Moby v25+ [release](https://docs.docker.com/engine/install/).
  - Updated `CreateMatrix` tooling to use the newest version for the `latest` tag when multiple versions are available.
- Version 2.0:
  - Added a build release [version](./version.json), this version is independent of Nx release versions, and only identifies the version of the build environment, and is used in the image label.
  - Nx released v5.1 across all product brands, v5.1 [supports][nx_os_support] Ubuntu Jammy 22.04 LTS, and all base images have been updated to Jammy.
  - Due to the Jammy dependency versions older than v5.1 are no longer being built.
  - Build scripts removed support for old v4 variants.
  - Added a link from `/root/.config/nx_ini` to `/config/ini` for additional INI configuration files.

## Releases

Images are published on [Docker Hub][hub]:

- [NxWitness][hub_nxwitness]: `docker pull docker.io/ptr727/nxwitness`
- [NxWitness-LSIO][hub_nxwitness-lsio]: `docker pull docker.io/ptr727/nxwitness-lsio`
- [NxMeta][hub_nxmeta]: `docker pull docker.io/ptr727/nxmeta`
- [NxMeta-LSIO][hub_nxmeta-lsio]: `docker pull docker.io/ptr727/nxmeta-lsio`
- [DWSpectrum][hub_dwspectrum]: `docker pull docker.io/ptr727/dwspectrum`
- [DWSpectrum-LSIO][hub_dwspectrum-lsio]: `docker pull docker.io/ptr727/dwspectrum-lsio`

Images are tagged as follows:

- `latest`: Latest published version, e.g. `docker pull docker.io/ptr727/nxmeta:latest`.
- `stable`: Latest released version, e.g. `docker pull docker.io/ptr727/nxmeta:stable`.
- `rc`: Latest RC version, e.g. `docker pull docker.io/ptr727/nxmeta:rc`.
- `beta`: Latest Beta version, e.g. `docker pull docker.io/ptr727/nxmeta:beta`
- `develop`: Builds created from the develop branch, e.g. `docker pull docker.io/ptr727/nxmeta:develop`.
- `[version]`: Release version number, e.g. `docker pull docker.io/ptr727/nxmeta:5.2.2.37996`.

Notes:

- `latest` and `stable` may be the same version if all builds are released builds.
- `rc` and `beta` tags are only built when RC and Beta builds are published by Nx, and may be older than current `latest` or `stable` builds.
- See [Build Process](#build-process) for determination of the "released" status of a build.

The images are updated weekly, picking up the latest upstream Ubuntu updates and newly released Nx product versions.  
See the [Build Process](#build-process) section for more details on how versions and builds are managed.

[NxWitness][hub_nxwitness]:  
[![NxWitness Stable][hub_nxwitness_stable_shield]][hub_nxwitness]
[![NxWitness Latest][hub_nxwitness_latest_shield]][hub_nxwitness]
[![NxWitness RC][hub_nxwitness_rc_shield]][hub_nxwitness]
[![NxWitness Beta][hub_nxwitness_beta_shield]][hub_nxwitness]

[NxWitness-LSIO][hub_nxwitness-lsio]:  
[![NxWitness-LSIO Stable][hub_nxwitness-lsio_stable_shield]][hub_nxwitness-lsio]
[![NxWitness-LSIO Latest][hub_nxwitness-lsio_latest_shield]][hub_nxwitness-lsio]
[![NxWitness-LSIO RC][hub_nxwitness-lsio_rc_shield]][hub_nxwitness-lsio]
[![NxWitness-LSIO Beta][hub_nxwitness-lsio_beta_shield]][hub_nxwitness-lsio]

[NxMeta][hub_nxmeta]:  
[![NxMeta Stable][hub_nxmeta_stable_shield]][hub_nxmeta]
[![NxMeta Latest][hub_nxmeta_latest_shield]][hub_nxmeta]
[![NxMeta RC][hub_nxmeta_rc_shield]][hub_nxmeta]
[![NxMeta Beta][hub_nxmeta_beta_shield]][hub_nxmeta]

[NxMeta-LSIO][hub_nxmeta-lsio]:  
[![NxMeta-LSIO Stable][hub_nxmeta-lsio_stable_shield]][hub_nxmeta-lsio]
[![NxMeta-LSIO Latest][hub_nxmeta-lsio_latest_shield]][hub_nxmeta-lsio]
[![NxMeta-LSIO RC][hub_nxmeta-lsio_rc_shield]][hub_nxmeta-lsio]
[![NxMeta-LSIO Beta][hub_nxmeta-lsio_beta_shield]][hub_nxmeta-lsio]

[DWSpectrum][hub_dwspectrum]:  
[![DWSpectrum Stable][hub_dwspectrum_stable_shield]][hub_dwspectrum]
[![DWSpectrum Latest][hub_dwspectrum_latest_shield]][hub_dwspectrum]
[![DWSpectrum RC][hub_dwspectrum_rc_shield]][hub_dwspectrum]
[![DWSpectrum Beta][hub_dwspectrum_beta_shield]][hub_dwspectrum]

[DWSpectrum-LSIO][hub_dwspectrum-lsio]:  
[![DWSpectrum-LSIO Stable][hub_dwspectrum-lsio_stable_shield]][hub_dwspectrum-lsio]
[![DWSpectrum-LSIO Latest][hub_dwspectrum-lsio_latest_shield]][hub_dwspectrum-lsio]
[![DWSpectrum-LSIO RC][hub_dwspectrum-lsio_rc_shield]][hub_dwspectrum-lsio]
[![DWSpectrum-LSIO Beta][hub_dwspectrum-lsio_beta_shield]][hub_dwspectrum-lsio]

## Overview

### Introduction

I ran DW Spectrum in my home lab on an Ubuntu Virtual Machine, and was looking for a way to run it in Docker. At the time Network Optix provided no support for Docker, but I did find the [The Home Repot NxWitness][thehomerepo] project, that inspired me to create this project.  
I started with individual repositories for Nx Witness, Nx Meta, and DW Spectrum, but that soon became cumbersome with lots of duplication, and I combined all product flavors into this one project.

Today Network Optix supports [Docker][nx_docker], and they publish [build scripts][nx_github_docker], but they do not publish container images.

### Products

The project supports three product variants:

- [Network Optix Nx Witness VMS][nxwitness].
- [Network Optix Nx Meta VMS][nxmeta], the developer test and preview version of Nx Witness.
- [Digital Watchdog DW Spectrum IPVMS][dwwatchdog], the US licensed and OEM branded version of Nx Witness.

### Base Images

The project creates two variants of each product using different base images:

- [Ubuntu][ubuntu] using [ubuntu:jammy][ubuntu_docker] base image.
- [LinuxServer][lsio] using [lsiobase/ubuntu:jammy][ubuntu_lsio_docker] base image.

Note that smaller base images like [Alpine][alpine] are not [supported][nx_os_support] by the mediaserver.

### LinuxServer

The [LinuxServer (LSIO)][lsio] base images provide valuable container functionality:

- The LSIO images are based on [s6-overlay][s6], are updated weekly, and LSIO [produces][lsio_fleet] containers for many popular open source applications.
- LSIO allows us to [specify][lsio_puid] the user account to use when running the mediaserver, while still running the `root-tool` as `root` (required for license enforcement).
- Running as non-root is a [best practice][docker_nonroot], and required if we need user specific permissions when accessing mapped volumes.
- The [nxvms-docker][nx_github_compose] project takes a different approach running a compose stack that runs the mediaserver in one instance under the `${COMPANY_NAME}` account, and the root-tool in a second instance under the `root` account, using a shared `/tmp` volume for socket IPC between the mediaserver and root-tool, but the user account `${COMPANY_NAME}` does not readily map to a user on the host system.

## Configuration

User accounts and directory names are based on the product variant exposed by the `${COMPANY_NAME}` variable:

- NxWitness: `networkoptix`
- DWSpectrum: `digitalwatchdog`
- NxMeta: `networkoptix-metavms`

### LSIO Volumes

The LSIO images [re-link](./LSIO/etc/s6-overlay/s6-rc.d/init-nx-relocate/run) various internal paths to `/config`.

- `/config` : Configuration files:
  - `/opt/${COMPANY_NAME}/mediaserver/etc` links to `/config/etc` : Configuration.
  - `/root/.config/nx_ini` links to `/config/ini` : Additional configuration.
  - `/opt/${COMPANY_NAME}/mediaserver/var` links to `/config/var` : State and logs.
- `/media` : Recording files.

### Non-LSIO Volumes

The non-LSIO images must be mapped directly to the installed paths, refer to the [nxvms-docker][nx_github_volumes] page for details.

- `/opt/${COMPANY_NAME}/mediaserver/etc` : Configuration.
- `/home/${COMPANY_NAME}/.config/nx_ini` : Additional configuration.
- `/opt/${COMPANY_NAME}/mediaserver/var` : State and logs.
- `/media` : Recording files.

### Ports

- `7001` : Default server port.

### Environment Variables

- `PUID` : User Id, LSIO only, optional.
- `PGID` : Group Id, LSIO only, optional.
- `TZ` : Timezone, e.g. `America/Los_Angeles`.

See [LSIO docs][lsio_puid] for usage of `PUID` and `PGID` that allow the mediaserver to run under a user account and the root-tool to run as root.

### Network Mode

Any network mode can be used, but due to the hardware bound licensing, `host` mode is [recommended][nx_github_networking].

## Examples

### LSIO Docker Create

```console
docker create \
  --name=nxwitness-lsio-test-container \
  --hostname=nxwitness-lsio-test-host \
  --domainname=foo.bar.net \
  --restart=unless-stopped \
  --network=host \
  --env TZ=America/Los_Angeles \
  --volume /mnt/nxwitness/config:/config:rw \
  --volume /mnt/nxwitness/media:/media:rw \
  docker.io/ptr727/nxwitness-lsio:stable

docker start nxwitness-lsio-test-container
```

### LSIO Docker Compose

```yaml
version: "3.7"

services:
  nxwitness:
    image: docker.io/ptr727/nxwitness-lsio:stable
    container_name: nxwitness-lsio-test-container
    restart: unless-stopped
    network_mode: host
    environment:
      # - PUID=65534 # id $user
      # - PGID=65534 # id $group
      - TZ=America/Los_Angeles
    volumes:
      - /mnt/nxwitness/config:/config
      - /mnt/nxwitness/media:/media
```

### Non-LSIO Docker Compose

```yaml
version: "3.7"

services:
  nxwitness:
    image: docker.io/ptr727/nxwitness:stable
    container_name: nxwitness-test-container
    restart: unless-stopped
    network_mode: host
    volumes:
      - /mnt/nxwitness/config/etc:/opt/networkoptix/mediaserver/etc
      - /mnt/nxwitness/config/nx_ini:/home/networkoptix/.config/nx_ini
      - /mnt/nxwitness/config/var:/opt/networkoptix/mediaserver/var
      - /mnt/nxwitness/media:/media
```

### Unraid Template

- Add the template [URL](./Unraid) `https://github.com/ptr727/NxWitness/tree/main/Unraid` to the "Template Repositories" section, at the bottom of the "Docker" configuration tab, and click "Save".
- Create a new container by clicking the "Add Container" button, select the desired product template from the dropdown.
- If using Unassigned Devices for media storage, use `RW/Slave` access mode.
- Use `nobody` and `users` identifiers, `PUID=99` and `PGID=100`.
- Register the Unraid filesystems in the `additionalLocalFsTypes` advanced settings, see the [Missing Storage](#missing-storage) section for help.

## Product Information

### Release Information

- Nx Witness:
  - [Downloads API](https://nxvms.com/api/utils/downloads)
  - [Releases API][nxwitness_releases]
  - [Downloads](https://nxvms.com/download/linux)
  - [Beta Downloads](https://beta.networkoptix.com/beta-builds/default)
  - [Release Notes](https://www.networkoptix.com/all-nx-witness-release-notes)
- Nx Meta:
  - [Downloads API](https://meta.nxvms.com/api/utils/downloads)
  - [Releases API](https://updates.vmsproxy.com/metavms/releases.json)
  - [Early Access Signup](https://support.networkoptix.com/hc/en-us/articles/360046713714-Get-an-Nx-Meta-Build)
  - [Request Developer Licenses](https://support.networkoptix.com/hc/en-us/articles/360045718294-Getting-Licenses-for-Developers)
  - [Downloads](https://meta.nxvms.com/download/linux)
  - [Beta Downloads](https://meta.nxvms.com/downloads/patches)
- DW Spectrum:
  - [Downloads API](https://dwspectrum.digital-watchdog.com/api/utils/downloads)
  - [Releases API](https://updates.vmsproxy.com/digitalwatchdog/releases.json)
  - [Downloads](https://dwspectrum.digital-watchdog.com/download/linux)
  - [Release Notes](https://digital-watchdog.com/DWSpectrum-Releasenote/DWSpectrum.html)

### Advanced Configuration

- `mediaserver.conf` [Configuration](https://support.networkoptix.com/hc/en-us/articles/360036389693-How-to-access-Nx-Server-configuration-options): `https://[hostname]:[port]/#/server-documentation`
- `nx_vms_server.ini` [Configuration](https://meta.nxvms.com/docs/developers/knowledgebase/241-configuring-via-ini-files--iniconfig): `https://[hostname]:[port]/api/iniConfig/`
- Advanced Server Configuration: `https://[hostname]:[port]/#/settings/advanced`
- Storage Reporting: `https://[hostname]:[port]/#/health/storages`

## Build Process

Build overview:

- [Build scripts](./Make/) are used to create the [`Dockerfile`'s](./Docker/) for all permutations of "Entrypoint", "LSIO", "NxMeta", "NxWitness" and "DWSpectrum" variants and products.
- Docker does [not support](https://github.com/moby/moby/issues/735) a native `include` directive, instead the [M4 macro processor](https://www.gnu.org/software/m4/) is used to assemble common snippets.
- The `Dockerfile` [downloads](./Docker/download.sh) and installs the mediaserver installer at build time using environment variables for the URLs.
- [`CreateMatrix`](./CreateMatrix/) is a custom app used to update available product versions and download URLs.
- [`Version.json`](./Make/Version.json) is updated using the mediaserver [Releases JSON API][nxwitness_releases] and [Packages API](https://updates.networkoptix.com/default/38363/packages.json).
- The logic follows the same pattern as used by the [Nx Open](https://github.com/networkoptix/nx_open/blob/master/vms/libs/nx_vms_update/src/nx/vms/update/releases_info.cpp) desktop client logic.
- The "released" status of a build follows the same method as Nx uses in [`isBuildPublished()`][isbuildpublished] where `release_date` and `release_delivery_days` from the [Releases JSON API][nxwitness_releases] must be greater than `0`
- [`Matrix.json`](./Make/Matrix.json) is created from the `Version.json` file and is used during pipeline builds using a [Matrix](https://docs.github.com/en/actions/using-jobs/using-a-matrix-for-your-jobs) strategy.
- Automated builds are done using [GitHub Actions](https://docs.github.com/en/actions) and the [`BuildPublishPipeline.yml`](./.github/workflows/BuildPublishPipeline.yml) pipeline.
- Version history is maintained and used by `CreateMatrix` such that generic tags, e.g. `latest`, will never result in a lesser version number, i.e. break-fix-forward only, see [Issue #62](https://github.com/ptr727/NxWitness/issues/62) for details on Nx re-publishing "released" builds using an older version breaking already upgraded systems.

Local testing:

- Run `cd ./Make` and [`./Test.sh`](./Make/Test.sh), the following will be executed:
  - [`Create.sh`](./Make/Create.sh): Create `Dockerfile`'s from the snippets using M4 and update the latest version information using `CreateMatrix`.
  - [`Build.sh`](./Make/Build.sh): Builds the `Dockerfile`'s using `docker buildx build`.
  - [`Up.sh`](./Make/Up.sh): Launch a docker compose stack [`Test.yaml`](./Make/Test.yml) to run all product variants.
- Ctrl-Click on the links to launch the web UI for each of the product variants.
- Run [`Clean.sh`](./Make/Clean.sh) to shutdown the compose stack and cleanup images.

## Known Issues

- Licensing:
  - Camera recording license keys are activated and bound to hardware attributes of the host server collected by the `root-tool` that is required to run as `root`.
  - Requiring the `root-tool` to run as root overly complicates running the `mediaserver` as a non-root user, and requires the container to run using `host` networking to not break the hardware license checks.
  - Docker containers are supposed to be portable, and moving containers between hosts will break license activation.
  - Nx to fix: Associate licenses with the [Cloud Account][nx_cloud] not the local hardware.
- Storage Management:
  - The mediaserver attempts to automatically decide what storage to use.
  - Filesystem types are filtered out if not on the [supported list][nx_github_storage].
  - Mounted volumes are ignored if backed by the same physical storage, even if logically separate.
  - Unwanted `Nx MetaVMS Media` directories are created on any discoverable writable storage.
  - Nx to fix: Eliminate the elaborate filesystem filter logic and use only the admin specified storage locations.
- Configuration Files:
  - `.conf` configuration files are located in a static `mediaserver/etc` location while `.ini` configuration files are in a user-account dependent location, e.g. `/home/networkoptix/.config/nx_ini` or `/root/.config/nx_ini`.
  - There is no value in having a server use per-user configuration directories, and it is inconsistent to mix configuration file locations.
  - Nx to fix: Store all configuration files in `mediaserver/etc`.
- External Plugins:
  - Custom or [Marketplace][nx_marketplace] plugins are installed in the `mediaserver/bin/plugins` directory.
  - The `mediaserver/bin/plugins` directory is already pre-populated with Nx installed plugins.
  - It is not possible to use external plugins from a mounted volume as the directory is already in-use.
  - Nx to fix: Load plugins from `mediaserver/var/plugins` or from sub-directories mounted below `mediaserver/bin/plugins`, e.g. `mediaserver/bin/plugins/external`
- Lifetime Upgrades:
  - Nx is a cloud product, free to view, free upgrades, comes with ongoing costs of hosting, maintenance, and support, it is [unfeasible][nx_crunchbase] to sustain a business with ongoing costs using perpetual one-off licenses.
  - My personal experience with [Digital Watchdog][digitalwatchdog] and their [Lifetime Upgrades and No Annual Agreements][dw_upgrades] is an inflexible policy of three activations per license and you have to buy a new license, thus the "license lifetime" is a multiplier of the "hardware lifetime".
  - Nx to fix: Yearly camera license renewals covering the cost of support and upgrades.
- Archiving:
  - Nx makes no distinction between recording and archiving storage, archive is basically just a recording mirror without any capacity or retention benefit.
  - Recording storage is typically high speed low latency high cost low capacity SSD/NVMe arrays, while archival playback storage is very high capacity low cost magnetic media arrays.
  - Nx to fix: Implement something akin to archiving in [Milestone XProtect VMS][milestone] where recording storage is separate from long term archival storage.
- Image Publication:
  - Nx relies on end-users or projects like this one to create and publish docker images.
  - Nx to fix: Publish up-to-date images for all product variants and release channels.
- Break-Fix-Version-Forward:
  - Nx product versions published via their releases API occasionally go backwards, e.g. `release`: v4.3 -> v5.0 -> v4.3.
  - Nx supports forward-only in-place upgrades, e.g. v4.3 to v5.0, but not v5.0 to v4.3.
  - Publishing generic tags, e.g. `latest`, using a version that regresses, e.g. v4.3 -> v5.0 -> v4.3 breaks deployments, see [Issue #62](https://github.com/ptr727/NxWitness/issues/62) for details.
  - `CreateMatrix` tooling keeps track of published versions, and prevents version regression of generic `latest`, `rc` and `beta` tags.
  - Nx to fix: Release break-fix-version-forward only via release API's.

## Troubleshooting

I am not affiliated with Network Optix, I cannot provide support for their products, please contact [Network Optix Support][nx_support] for product support issues.  
If there are issues with the docker build scripts used in this project, please create a [GitHub Issue](https://github.com/ptr727/NxWitness/issues).  
Note that I only test and run `nxmeta-lsio:stable` in my home lab, other images get very little to no testing, please test accordingly.

### Missing Storage

The following section will help troubleshoot common problems with missing storage.  
If this does not help, please contact [Network Optix Support][nx_support].  
Please do not open a GitHub issue unless you are positive the issue is with the `Dockerfile`.

Confirm that all the mounted volumes are listed in the available storage locations in the [web admin][nx_webadmin] portal.

Enable [debug logging][nx_debuglogging] in the mediaserver:  
Edit `mediaserver.conf`, set `logLevel=verbose`, restart the server.  
Look for clues in `/config/var/log/log_file.log`.

E.g.

```log
VERBOSE nx::vms::server::fs: shfs /media fuse.shfs - duplicate
VERBOSE nx::vms::server::fs: /dev/sdb8 /media btrfs - duplicate
DEBUG QnStorageSpaceRestHandler(0x7f85043b0b00): Return 0 storages and 1 protocols
```

Get a list of the mapped volume mounts in the running container, and verify that `/config` and `/media` are listed in the `Mounts` section:

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

Example output for ZFS (note that ZFS support [was added][nx_releasenotes] in v5.0):

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

Add the required filesystem types in the [advanced configuration](#advanced-configuration) menu.
Edit the `additionalLocalFsTypes` option and add the required filesystem types, e.g. `fuse.shfs,btrfs,zfs`, restart the server.

Alternatively call the configuration API directly:  
`wget --no-check-certificate --user=[username] --password=[password] https://[hostname]:[port]/api/systemSettings?additionalLocalFsTypes=fuse.shfs,btrfs,zfs`.

To my knowledge there is no solution to duplicate devices being filtered, please contact [Network Optix Support][nx_support] and ask them to stop filtering filesystem types and devices.

[actions]: https://github.com/ptr727/NxWitness/actions
[alpine]: https://alpinelinux.org/
[digitalwatchdog]: https://digital-watchdog.com/
[docker_nonroot]: https://docs.docker.com/develop/develop-images/dockerfile_best-practices/#user
[dw_upgrades]: https://dwspectrum.com/upgrades/
[dwwatchdog]: https://digital-watchdog.com/productdetail/DW-Spectrum-IPVMS/
[hub_dwspectrum_beta_shield]: https://img.shields.io/docker/v/ptr727/dwspectrum/beta?label=beta&logo=docker
[hub_dwspectrum_latest_shield]: https://img.shields.io/docker/v/ptr727/dwspectrum/latest?label=latest&logo=docker
[hub_dwspectrum_rc_shield]: https://img.shields.io/docker/v/ptr727/dwspectrum/rc?label=rc&logo=docker
[hub_dwspectrum_stable_shield]: https://img.shields.io/docker/v/ptr727/dwspectrum/stable?label=stable&logo=docker
[hub_dwspectrum-lsio_beta_shield]: https://img.shields.io/docker/v/ptr727/dwspectrum-lsio/beta?label=beta&logo=docker
[hub_dwspectrum-lsio_latest_shield]: https://img.shields.io/docker/v/ptr727/dwspectrum-lsio/latest?label=latest&logo=docker
[hub_dwspectrum-lsio_rc_shield]: https://img.shields.io/docker/v/ptr727/dwspectrum-lsio/rc?label=rc&logo=docker
[hub_dwspectrum-lsio_stable_shield]: https://img.shields.io/docker/v/ptr727/dwspectrum-lsio/stable?label=stable&logo=docker
[hub_dwspectrum-lsio]: https://hub.docker.com/r/ptr727/dwspectrum-lsio
[hub_dwspectrum]: https://hub.docker.com/r/ptr727/dwspectrum
[hub_nxmeta_beta_shield]: https://img.shields.io/docker/v/ptr727/nxmeta/beta?label=beta&logo=docker
[hub_nxmeta_latest_shield]: https://img.shields.io/docker/v/ptr727/nxmeta/latest?label=latest&logo=docker
[hub_nxmeta_rc_shield]: https://img.shields.io/docker/v/ptr727/nxmeta/rc?label=rc&logo=docker
[hub_nxmeta_stable_shield]: https://img.shields.io/docker/v/ptr727/nxmeta/stable?label=stable&logo=docker
[hub_nxmeta-lsio_beta_shield]: https://img.shields.io/docker/v/ptr727/nxmeta-lsio/beta?label=beta&logo=docker
[hub_nxmeta-lsio_latest_shield]: https://img.shields.io/docker/v/ptr727/nxmeta-lsio/latest?label=latest&logo=docker
[hub_nxmeta-lsio_rc_shield]: https://img.shields.io/docker/v/ptr727/nxmeta-lsio/rc?label=rc&logo=docker
[hub_nxmeta-lsio_stable_shield]: https://img.shields.io/docker/v/ptr727/nxmeta-lsio/stable?label=stable&logo=docker
[hub_nxmeta-lsio]: https://hub.docker.com/r/ptr727/nxmeta-lsio
[hub_nxmeta]: https://hub.docker.com/r/ptr727/nxmeta
[hub_nxwitness_beta_shield]: https://img.shields.io/docker/v/ptr727/nxwitness/beta?label=beta&logo=docker
[hub_nxwitness_latest_shield]: https://img.shields.io/docker/v/ptr727/nxwitness/latest?label=latest&logo=docker
[hub_nxwitness_rc_shield]: https://img.shields.io/docker/v/ptr727/nxwitness/rc?label=rc&logo=docker
[hub_nxwitness_stable_shield]: https://img.shields.io/docker/v/ptr727/nxwitness/stable?label=stable&logo=docker
[hub_nxwitness-lsio_beta_shield]: https://img.shields.io/docker/v/ptr727/nxwitness-lsio/beta?label=beta&logo=docker
[hub_nxwitness-lsio_latest_shield]: https://img.shields.io/docker/v/ptr727/nxwitness-lsio/latest?label=latest&logo=docker
[hub_nxwitness-lsio_rc_shield]: https://img.shields.io/docker/v/ptr727/nxwitness-lsio/rc?label=rc&logo=docker
[hub_nxwitness-lsio_stable_shield]: https://img.shields.io/docker/v/ptr727/nxwitness-lsio/stable?label=stable&logo=docker
[hub_nxwitness-lsio]: https://hub.docker.com/r/ptr727/nxwitness-lsio
[hub_nxwitness]: https://hub.docker.com/r/ptr727/nxwitness
[hub]: https://hub.docker.com/u/ptr727
[isbuildpublished]: https://github.com/networkoptix/nx_open/blob/526967920636d3119c92a5220290ecc10957bf12/vms/libs/nx_vms_update/src/nx/vms/update/releases_info.cpp#L31
[last_build_shield]: https://byob.yarr.is/ptr727/NxWitness/lastbuild
[last_commit_shield]: https://img.shields.io/github/last-commit/ptr727/NxWitness?logo=github
[license_shield]: https://img.shields.io/github/license/ptr727/NxWitness
[license]: ./LICENSE
[lsio_fleet]: https://fleet.linuxserver.io/
[lsio_puid]: https://docs.linuxserver.io/general/understanding-puid-and-pgid
[lsio]: https://www.linuxserver.io/
[milestone]: https://doc.milestonesys.com/latest/en-US/standard_features/sf_mc/sf_systemoverview/mc_storageandarchivingexplained.htm
[nx_cloud]: https://www.networkoptix.com/nx-witness/nx-witness-cloud/
[nx_crunchbase]: https://www.crunchbase.com/organization/network-optix
[nx_debuglogging]: https://support.networkoptix.com/hc/en-us/articles/236033688-How-to-change-software-logging-level-and-how-to-get-logs
[nx_docker]: https://support.networkoptix.com/hc/en-us/articles/360037973573-Docker
[nx_github_compose]: https://github.com/networkoptix/nxvms-docker/blob/master/docker-compose.yaml
[nx_github_docker]: https://github.com/networkoptix/nxvms-docker
[nx_github_networking]: https://github.com/networkoptix/nxvms-docker#networking
[nx_github_storage]: https://github.com/networkoptix/nxvms-docker#notes-about-storage
[nx_github_volumes]: https://github.com/networkoptix/nxvms-docker#volumes-description
[nx_marketplace]: https://www.networkoptix.com/nx-meta/nx-integrations-marketplace/
[nx_os_support]: https://support.networkoptix.com/hc/en-us/articles/205313168-Nx-Witness-Operating-System-Support
[nx_releasenotes]: https://support.networkoptix.com/hc/en-us/articles/360042751193-Current-and-Past-Releases-Downloads-Release-Notes
[nx_support]: https://support.networkoptix.com/hc/en-us/community/topics
[nx_webadmin]: https://support.networkoptix.com/hc/en-us/articles/115012831028-Nx-Server-Web-Admin
[nxmeta]: https://meta.nxvms.com/
[nxwitness_releases]: https://updates.vmsproxy.com/default/releases.json
[nxwitness]: https://www.networkoptix.com/nx-witness/
[repo]: https://github.com/ptr727/NxWitness
[s6]: https://github.com/just-containers/s6-overlay
[thehomerepo]: https://github.com/thehomerepot/nxwitness
[ubuntu_docker]: https://hub.docker.com/_/ubuntu
[ubuntu_lsio_docker]: https://hub.docker.com/r/lsiobase/ubuntu
[ubuntu]: https://ubuntu.com/
[workflow_status_shield]: https://img.shields.io/github/actions/workflow/status/ptr727/NxWitness/BuildPublishPipeline.yml?branch=main&logo=github
