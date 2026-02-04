# Docker Projects for Network Optix VMS Products

This is a project to build and publish docker images for various [Network Optix][networkoptix-link] VMS products.

## Build and Distribution

### Build Status

[![Release Status][releasebuildstatus-shield]][actions-link]\
[![Last Commit][lastcommit-shield]][github-link]\
[![Last Build][lastbuild-shield]][actions-link]

### Release Notes

**Version: 2.9**:

**Summary**:

- Refactoring to match layout and style used in other projects.
- Updated to .NET 10, adding support for Nullable and AOT.

See [Release History](./HISTORY.md) for complete release notes and older versions.

## Getting Started

**Getting started with a simple test compose file**:

```yaml
# compose.yaml

# Test using non persistent docker volumes
volumes:
  test_nxwitness-lsio_config:
  test_nxwitness-lsio_media:
  test_nxwitness-lsio_backup:
  test_nxwitness-lsio_analytics:

services:
  nxwitness-lsio:
    # Use the image matching your product
    image: docker.io/ptr727/nxwitness-lsio:stable
    container_name: nxwitness-lsio-test-container
    restart: unless-stopped
    network_mode: bridge
    ports:
      # Expose the service on port 7203
      - 7203:7001
    environment:
      - TZ=America/Los_Angeles
    volumes:
      # Map to your real storage in production
      - test_nxwitness-lsio_config:/config
      - test_nxwitness-lsio_media:/media
      - test_nxwitness-lsio_backup:/backup
      - test_nxwitness-lsio_analytics:/analytics
```

```shell
# Launch the service
docker compose up --detach

# Open your web browser on the local machine port 7203
echo "Nx Witness LSIO:" "https://$HOSTNAME:7203/"

# Shut the service down
docker compose down
```

**Example of a service in production**:

```yaml
networks:

  public_network: # External macvlan network
    name: ${PUBLIC_NETWORK_NAME}
    external: true
  local_network: # External bridge network
    name: ${LOCAL_NETWORK_NAME}
    external: true
  stack_network: # Stack network


services:

  nxmeta:
    image: docker.io/ptr727/nxmeta-lsio:latest
    container_name: nxmeta
    hostname: nxmeta
    domainname: ${DOMAIN_NAME}
    restart: unless-stopped
    user: root
    group_add:
      - ${DOCKER_GROUP_ID}
    security_opt: # Set with care
      - seccomp=unconfined
      - apparmor=unconfined
    environment:
      - TZ=${TZ}
      - PUID=${USER_NONROOT_ID} # Run as non-root user
      - PGID=${USERS_GROUP_ID}
    volumes: # ZFS volumes
      - ${APPDATA_DIR}/nxmeta/config:/config
      - ${NVR_DIR}/media:/media
      - ${NVR_DIR}/backup:/backup
      - ${NVR_DIR}/analytics:/analytics
    networks:
      public_network:
        ipv4_address: ${NXMETA_IP} # Static IP
        mac_address: ${NXMETA_MAC} # Static MAC
      local_network:
      stack_network:
    labels:
      - traefik.enable=true # Traefik SSL proxy
      - traefik.http.routers.nxmeta.rule=HostRegexp(`^nxmeta${DOMAIN_REGEX}$$`)
      - traefik.http.services.nxmeta.loadbalancer.server.scheme=https
      - traefik.http.services.nxmeta.loadbalancer.server.port=7001
```

## Table of Contents

- [Build and Distribution](#build-and-distribution)
  - [Build Status](#build-status)
  - [Release Notes](#release-notes)
- [Getting Started](#getting-started)
- [Table of Contents](#table-of-contents)
- [Products](#products)
- [Releases](#releases)
- [Overview](#overview)
  - [Introduction](#introduction)
  - [Base Images](#base-images)
  - [LinuxServer](#linuxserver)
- [Configuration](#configuration)
  - [LSIO Volumes](#lsio-volumes)
  - [Non-LSIO Volumes](#non-lsio-volumes)
  - [Ports](#ports)
  - [Environment Variables](#environment-variables)
  - [Network Mode](#network-mode)
- [Examples](#examples)
  - [LSIO Docker Create](#lsio-docker-create)
  - [LSIO Docker Compose](#lsio-docker-compose)
  - [Non-LSIO Docker Compose](#non-lsio-docker-compose)
  - [Unraid Template](#unraid-template)
- [Product Information](#product-information)
  - [Release Information](#release-information)
  - [Advanced Configuration](#advanced-configuration)
- [Build Process](#build-process)
- [Known Issues](#known-issues)
- [Troubleshooting](#troubleshooting)
  - [Missing Storage](#missing-storage)
- [License](#license)

## Products

The project supports the following product variants:

- [Network Optix][networkoptix-link] [Nx Witness VMS][nxwitness-link] (not available for purchase in the US)
- [Network Optix][networkoptix-link] [Nx Meta VMS][nxmeta-link] (developer and early access version of Nx Witness)
- [Network Optix][networkoptix-link] [Nx Go VMS][nxgo-link] (version of Nx Witness targeted at transportation sector)
- [Digital Watchdog][digitalwatchdog-link] [DW Spectrum IPVMS][dwspectrum-link] (US licensed and OEM branded version of Nx Witness)
- [Hanwha Vision][hanwhavision-link] [Wisenet WAVE VMS][dwspectrum-link] (US licensed and OEM branded version of Nx Witness)

## Releases

Images are published on [Docker Hub][hub-link]:

- [NxWitness][hubnxwitness-link]: `docker pull docker.io/ptr727/nxwitness`
- [NxWitness-LSIO][hubnxwitnesslsio-link]: `docker pull docker.io/ptr727/nxwitness-lsio`
- [NxMeta][hubnxmeta-link]: `docker pull docker.io/ptr727/nxmeta`
- [NxMeta-LSIO][hubnxmetalsio-link]: `docker pull docker.io/ptr727/nxmeta-lsio`
- [NxGo][hubnxgo-link]: `docker pull docker.io/ptr727/nxgo`
- [NxGo-LSIO][hubnxwitnesslsio-link]: `docker pull docker.io/ptr727/nxgo-lsio`
- [DWSpectrum][hubdwspectrum-link]: `docker pull docker.io/ptr727/dwspectrum`
- [DWSpectrum-LSIO][hubdwspectrumlsio-link]: `docker pull docker.io/ptr727/dwspectrum-lsio`
- [WisenetWAVE][hubwisenetwave-link]: `docker pull docker.io/ptr727/wisenetwave`
- [WisenetWAVE-LSIO][hubwisenetwavelsio-link]: `docker pull docker.io/ptr727/wisenetwave-lsio`

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
- Images are updated weekly, picking up the latest upstream Ubuntu updates and newly released Nx product versions.
- See [Build Process](#build-process) for more details.

**Docker releases**:

[NxWitness][hubnxwitness-link]:\
[![NxWitness Stable][hubnxwitnessstable-shield]][hubnxwitness-link]
[![NxWitness Latest][hubnxwitnesslatest-shield]][hubnxwitness-link]
[![NxWitness RC][hubnxwitnessrc-shield]][hubnxwitness-link]
[![NxWitness Beta][hubnxwitnessbeta-shield]][hubnxwitness-link]

[NxWitness-LSIO][hubnxwitnesslsio-link]:\
[![NxWitness-LSIO Stable][hubnxwitnesslsiostable-shield]][hubnxwitnesslsio-link]
[![NxWitness-LSIO Latest][hubnxwitnesslsiolatest-shield]][hubnxwitnesslsio-link]
[![NxWitness-LSIO RC][hubnxwitnesslsiorc-shield]][hubnxwitnesslsio-link]
[![NxWitness-LSIO Beta][hubnxwitnesslsiobeta-shield]][hubnxwitnesslsio-link]

[NxMeta][hubnxmeta-link]:\
[![NxMeta Stable][hubnxmetastable-shield]][hubnxmeta-link]
[![NxMeta Latest][hubnxmetalatest-shield]][hubnxmeta-link]
[![NxMeta RC][hubnxmetarc-shield]][hubnxmeta-link]
[![NxMeta Beta][hubnxmetabeta-shield]][hubnxmeta-link]

[NxMeta-LSIO][hubnxmetalsio-link]:\
[![NxMeta-LSIO Stable][hubnxmetalsiostable-shield]][hubnxmetalsio-link]
[![NxMeta-LSIO Latest][hubnxmetalsiolatest-shield]][hubnxmetalsio-link]
[![NxMeta-LSIO RC][hubnxmetalsiorc-shield]][hubnxmetalsio-link]
[![NxMeta-LSIO Beta][hubnxmetalsiobeta-shield]][hubnxmetalsio-link]

[NxGo][hubnxgo-link]:\
[![NxGo Stable][hubnxgostable-shield]][hubnxgo-link]
[![NxGo Latest][hubnxgolatest-shield]][hubnxgo-link]
[![NxGo RC][hubnxgorc-shield]][hubnxgo-link]
[![NxGo Beta][hubnxgobeta-shield]][hubnxgo-link]

[NxGo-LSIO][hubnxgolsio-link]:\
[![NxGo-LSIO Stable][hubnxgolsiostable-shield]][hubnxgolsio-link]
[![NxGo-LSIO Latest][hubnxgolsiolatest-shield]][hubnxgolsio-link]
[![NxGo-LSIO RC][hubnxgolsiorc-shield]][hubnxgolsio-link]
[![NxGo-LSIO Beta][hubnxgolsiobeta-shield]][hubnxgolsio-link]

[DWSpectrum][hubdwspectrum-link]:\
[![DWSpectrum Stable][hubdwspectrumstable-shield]][hubdwspectrum-link]
[![DWSpectrum Latest][hubdwspectrumlatest-shield]][hubdwspectrum-link]
[![DWSpectrum RC][hubdwspectrumrc-shield]][hubdwspectrum-link]
[![DWSpectrum Beta][hubdwspectrumbeta-shield]][hubdwspectrum-link]

[DWSpectrum-LSIO][hubdwspectrumlsio-link]:\
[![DWSpectrum-LSIO Stable][hubdwspectrumlsiostable-shield]][hubdwspectrumlsio-link]
[![DWSpectrum-LSIO Latest][hubdwspectrumlsiolatest-shield]][hubdwspectrumlsio-link]
[![DWSpectrum-LSIO RC][hubdwspectrumlsiorc-shield]][hubdwspectrumlsio-link]
[![DWSpectrum-LSIO Beta][hubdwspectrumlsiobeta-shield]][hubdwspectrumlsio-link]

[WisenetWAVE][hubwisenetwave-link]:\
[![WisenetWAVE Stable][hubwisenetwavestable-shield]][hubwisenetwave-link]
[![WisenetWAVE Latest][hubwisenetwavelatest-shield]][hubwisenetwave-link]
[![WisenetWAVE RC][hubwisenetwaverc-shield]][hubwisenetwave-link]
[![WisenetWAVE Beta][hubwisenetwavebeta-shield]][hubwisenetwave-link]

[WisenetWAVE-LSIO][hubwisenetwavelsio-link]:\
[![WisenetWAVE-LSIO Stable][hubwisenetwavelsiostable-shield]][hubwisenetwavelsio-link]
[![WisenetWAVE-LSIO Latest][hubwisenetwavelsiolatest-shield]][hubwisenetwavelsio-link]
[![WisenetWAVE-LSIO RC][hubwisenetwavelsiorc-shield]][hubwisenetwavelsio-link]
[![WisenetWAVE-LSIO Beta][hubwisenetwavelsiobeta-shield]][hubwisenetwavelsio-link]

## Overview

### Introduction

I ran DW Spectrum in my home lab on an Ubuntu Virtual Machine, and was looking for a way to run it in Docker. At the time Network Optix provided no support for Docker, but I did find the [The Home Repot NxWitness][thehomegithub-link] project, that inspired me to create this project.\
I started with individual repositories for Nx Witness, Nx Meta, and DW Spectrum, but that soon became cumbersome with lots of duplication, and I combined all product flavors into this one project.

Today Network Optix supports [Docker][nxdocker-link], and they publish [build scripts][nxgithubdocker-link], but they do not publish container images.

### Base Images

The project creates two variants of each product using different base images:

- [Ubuntu][ubuntu-link] using [ubuntu:jammy][ubuntudocker-link] base image.
- [LinuxServer][lsio-link] using [lsiobase/ubuntu:jammy][ubuntulsiodocker-link] base image.

Note that smaller base images like [Alpine][alpine-link] are not [supported][nxossupport-link] by the mediaserver.

### LinuxServer

The [LinuxServer (LSIO)][lsio-link] base images provide valuable container functionality:

- The LSIO images are based on [s6-overlay][s6overlay-link], are updated weekly, and LSIO [produces][lsiofleet-link] containers for many popular open source applications.
- LSIO allows us to [specify][lsiopuid-link] the user account to use when running the mediaserver, while still running the `root-tool` as `root` (required for license enforcement).
- Running as non-root is a [best practice][dockernonroot-link], and required if we need user specific permissions when accessing mapped volumes.
- The [nxvms-docker][nxgithubcompose-link] project takes a different approach running a compose stack that runs the mediaserver in one instance under the `${COMPANY_NAME}` account, and the root-tool in a second instance under the `root` account, using a shared `/tmp` volume for socket IPC between the mediaserver and root-tool, but the user account `${COMPANY_NAME}` does not readily map to a user on the host system.

## Configuration

User accounts and directory names are based on the product variant exposed by the `${COMPANY_NAME}` variable:

- NxWitness: `networkoptix`
- DWSpectrum: `digitalwatchdog`
- NxMeta: `networkoptix-metavms`
- WisenetWAVE: `hanwha`

### LSIO Volumes

The LSIO images [re-link](./LSIO/etc/s6-overlay/s6-rc.d/init-nx-relocate/run) various internal paths to `/config`.

- `/config` : Configuration files:
  - `/opt/${COMPANY_NAME}/mediaserver/etc` links to `/config/etc` : Configuration.
  - `/root/.config/nx_ini` links to `/config/ini` : Additional configuration.
  - `/opt/${COMPANY_NAME}/mediaserver/var` links to `/config/var` : State and logs.
- `/media` : Recording files.

### Non-LSIO Volumes

The non-LSIO images must be mapped directly to the installed paths, refer to the [nxvms-docker][nxgithubvolumes-link] page for details.

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

See [LSIO docs][lsiopuid-link] for usage of `PUID` and `PGID` that allow the mediaserver to run under a user account and the root-tool to run as root.

### Network Mode

Any network mode can be used, but due to the hardware bound licensing, `host` mode is [recommended][nxgithubnetworking-link].

## Examples

### LSIO Docker Create

```shell
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
  - [Releases JSON API][nxwitnessreleases-link]
  - [Downloads][nxwitnessdownload-link]
  - [Beta Downloads][nxwitnessbetadownload-link]
  - [Release Notes][nxwitnessreleasenotes-link]
- Nx Meta:
  - [Releases JSON API][nxmetareleases-link]
  - [Signup for Nx Meta][getstartedwithmeta-link]
  - [Request Developer Licenses][getalicense-link]
  - [Downloads][nxmetadownload-link]
  - [Beta Downloads][nxmetabetadownload-link]
- Nx Go:
  - [Releases JSON API][nxgoreleases-link]
  - [Downloads][nxgodownload-link]
  - [Beta Downloads][nxgobetadownload-link]
  - [Release Notes][nxgoreleasenotes-link]
- DW Spectrum:
  - [Releases JSON API][dwspectrumreleases-link]
  - [Downloads][dwspectrumdownload-link]
  - [Release Notes][dwspectrumreleasenotes-link]
- Wisenet WAVE:
  - [Releases JSON API][wisenetwavereleases-link]
  - [Downloads][wisenetwavedownload-link]
  - [Release Notes][wisenetwavereleasenotes-link]

### Advanced Configuration

- `mediaserver.conf` [Configuration][configoptions-link]: `https://[hostname]:[port]/#/server-documentation`
- `nx_vms_server.ini` [Configuration][iniconfig-link]: `https://[hostname]:[port]/api/iniConfig/`
- Advanced Server Configuration: `https://[hostname]:[port]/#/settings/advanced`
- Storage Reporting: `https://[hostname]:[port]/#/health/storages`

## Build Process

**Build overview**:

- [`CreateMatrix`](./CreateMatrix/) is used to update available product versions, and to create Docker files for all product permutations.
- [`Version.json`](./Make/Version.json) is updated using the mediaserver [Releases JSON API][nxwitnessreleases-link] and [Packages API][packages-link].
- The logic follows the same pattern as used by the [Nx Open][releaseinfo-link] desktop client logic.
- The "released" status of a build follows the same method as Nx uses in [`isBuildPublished()`][isbuildpublished-link] where `release_date` and `release_delivery_days` from the [Releases JSON API][nxwitnessreleases-link] must be greater than `0`
- [`Matrix.json`](./Make/Matrix.json) is created from the `Version.json` file and is used during pipeline builds using a [Matrix][matrix-link] strategy.
- Automated builds are done using [GitHub Actions](https://docs.github.com/en/actions) and the [`BuildPublishPipeline.yml`](./.github/workflows/BuildPublishPipeline.yml) pipeline.
- Version history is maintained and used by `CreateMatrix` such that generic tags, e.g. `latest`, will never result in a lesser version number, i.e. break-fix-forward only, see [Issue #62](https://github.com/ptr727/NxWitness/issues/62) for details on Nx re-publishing "released" builds using an older version breaking already upgraded systems.

**Local testing**:

- Run `cd ./Make` and [`./Test.sh`](./Make/Test.sh), the following will be executed:
  - [`Create.sh`](./Make/Create.sh): Create `Dockerfile`'s and update the latest version information using `CreateMatrix`.
  - [`Build.sh`](./Make/Build.sh): Builds the `Dockerfile`'s using `docker buildx build`.
  - [`Up.sh`](./Make/Up.sh): Launch a docker compose stack [`Test.yaml`](./Make/Test.yml) to run all product variants.
- Ctrl-Click on the links to launch the web UI for each of the product variants.
- Run [`Clean.sh`](./Make/Clean.sh) to shutdown the compose stack and cleanup images.

## Known Issues

- Licensing:
  - Camera recording license keys are activated and bound to hardware attributes of the host server collected by the `root-tool` that is required to run as `root`.
  - Requiring the `root-tool` to run as root overly complicates running the `mediaserver` as a non-root user, and requires the container to run using `host` networking to not break the hardware license checks.
  - Docker containers are supposed to be portable, and moving containers between hosts will break license activation.
  - Nx to fix: Associate licenses with the [Cloud Account][nxcloud-link] not the local hardware.
- Storage Management:
  - The mediaserver attempts to automatically decide what storage to use.
  - Filesystem types are filtered out if not on the [supported list][nxgithubstorage-link].
  - Mounted volumes are ignored if backed by the same physical storage, even if logically separate.
  - Unwanted `Nx MetaVMS Media` directories are created on any discoverable writable storage.
  - Nx to fix: Eliminate the elaborate filesystem filter logic and use only the admin specified storage locations.
- Configuration Files:
  - `.conf` configuration files are located in a static `mediaserver/etc` location while `.ini` configuration files are in a user-account dependent location, e.g. `/home/networkoptix/.config/nx_ini` or `/root/.config/nx_ini`.
  - There is no value in having a server use per-user configuration directories, and it is inconsistent to mix configuration file locations.
  - Nx to fix: Store all configuration files in `mediaserver/etc`.
- External Plugins:
  - Custom or [Marketplace][nxmarketplace-link] plugins are installed in the `mediaserver/bin/plugins` directory.
  - The `mediaserver/bin/plugins` directory is already pre-populated with Nx installed plugins.
  - It is not possible to use external plugins from a mounted volume as the directory is already in-use.
  - Nx to fix: Load plugins from `mediaserver/var/plugins` or from sub-directories mounted below `mediaserver/bin/plugins`, e.g. `mediaserver/bin/plugins/external`
- Lifetime Upgrades:
  - Nx is a cloud product, free to view, free upgrades, comes with ongoing costs of hosting, maintenance, and support, it is [unfeasible][nxcrunchbase-link] to sustain a business with ongoing costs using perpetual one-off licenses.
  - My personal experience with [Digital Watchdog][digitalwatchdog-link] and their [Lifetime Upgrades and No Annual Agreements][dwupgrades-link] is an inflexible policy of three activations per license and you have to buy a new license, thus the "license lifetime" is a multiplier of the "hardware lifetime".
  - Nx to fix: Yearly camera license renewals covering the cost of support and upgrades.
- Archiving:
  - Nx makes no distinction between recording and archiving storage, archive is basically just a recording mirror without any capacity or retention benefit.
  - Recording storage is typically high speed low latency high cost low capacity SSD/NVMe arrays, while archival playback storage is very high capacity low cost magnetic media arrays.
  - Nx to fix: Implement something akin to archiving in [Milestone XProtect VMS][milestone-link] where recording storage is separate from long term archival storage.
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

I am not affiliated with Network Optix, I cannot provide support for their products, please contact [Network Optix Support][nxsupport-link] for product support issues.\
If there are issues with the docker build scripts used in this project, please create a [GitHub Issue][issues-link].\
Note that I only test and run `nxmeta-lsio:stable` in my home lab, other images get very little to no testing, please test accordingly.

### Missing Storage

The following section will help troubleshoot common problems with missing storage.\
If this does not help, please contact [Network Optix Support][nxsupport-link].\
Please do not open a GitHub issue unless you are positive the issue is with the `Dockerfile`.

Confirm that all the mounted volumes are listed in the available storage locations in the [web admin][nxwebadmin-link] portal.

Enable [debug logging][nxdebuglogging-link] in the mediaserver:\
Edit `mediaserver.conf`, set `logLevel=verbose`, restart the server.\
Look for clues in `/config/var/log/log_file.log`.

E.g.

```log
VERBOSE nx::vms::server::fs: shfs /media fuse.shfs - duplicate
VERBOSE nx::vms::server::fs: /dev/sdb8 /media btrfs - duplicate
DEBUG QnStorageSpaceRestHandler(0x7f85043b0b00): Return 0 storages and 1 protocols
```

Get a list of the mapped volume mounts in the running container, and verify that `/config` and `/media` are listed in the `Mounts` section:

```shell
docker ps --no-trunc
docker container inspect [containername]
```

Launch a shell in the running container and get a list of filesystems mounts:

```shell
docker ps --no-trunc
docker exec --interactive --tty [containername] /bin/bash
cat /proc/mounts
exit
```

Example output for ZFS (note that ZFS support [was added][nxreleasenotes-link] in v5.0):

```shell
ssdpool/appdata /config zfs rw,noatime,xattr,posixacl 0 0
nvrpool/nvr /media zfs rw,noatime,xattr,posixacl 0 0
ssdpool/docker /archive zfs rw,noatime,xattr,posixacl 0 0
```

Mount `/config` is on device `ssdpool/appdata` and filesystem is `zfs`.\
Mount `/media` is on device `nvrpool/nvr` and filesystem is `zfs`.\
Mount `/archive` is on device `ssdpool/docker` and filesystem is `zfs`.

In this case the devices are unique and will not be filtered, but `zfs` is not supported and needs to be registered.

Example output for UnRaid FUSE:

```shell
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

The `/archive` mount is classified as a duplicate and ignored, map just `/media`, do not map `/archive`.\
Alternative use the "Unassigned Devices" plugin and dedicate e.g. a XFS formatted SSD drive to `/media` and/or `/config`.

Example output for Unraid BTRFS:

```shell
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

Alternatively call the configuration API directly:\
`wget --no-check-certificate --user=[username] --password=[password] https://[hostname]:[port]/api/systemSettings?additionalLocalFsTypes=fuse.shfs,btrfs,zfs`.

To my knowledge there is no solution to duplicate devices being filtered, please contact [Network Optix Support][nxsupport-link] and ask them to stop filtering filesystem types and devices.

## License

Licensed under the [MIT License][license-link]\
![GitHub License][license-shield]

[actions-link]: https://github.com/ptr727/NxWitness/actions
[alpine-link]: https://alpinelinux.org/
[configoptions-link]: https://support.networkoptix.com/hc/en-us/articles/360036389693-How-to-access-Nx-Server-configuration-options
[digitalwatchdog-link]: https://digital-watchdog.com/
[dockernonroot-link]: https://docs.docker.com/develop/develop-images/dockerfile_best-practices/#user
[dwspectrum-link]: https://dwspectrum.com/
[dwspectrumdownload-link]: https://dwspectrum.digital-watchdog.com/download/linux
[dwspectrumreleasenotes-link]: https://digital-watchdog.com/DWSpectrum-Releasenote/DWSpectrum.html
[dwspectrumreleases-link]: https://updates.vmsproxy.com/digitalwatchdog/releases.json
[dwupgrades-link]: https://dwspectrum.com/upgrades/
[getalicense-link]: https://support.networkoptix.com/hc/en-us/articles/8693698259607-Get-a-License-for-Developers
[getstartedwithmeta-link]: https://www.networkoptix.com/nx-meta/get-started-with-meta
[github-link]: https://github.com/ptr727/NxWitness
[hanwhavision-link]: https://hanwhavisionamerica.com/
[hub-link]: https://hub.docker.com/u/ptr727
[hubdwspectrum-link]: https://hub.docker.com/r/ptr727/dwspectrum
[hubdwspectrumbeta-shield]: https://img.shields.io/docker/v/ptr727/dwspectrum/beta?label=beta&logo=docker
[hubdwspectrumlatest-shield]: https://img.shields.io/docker/v/ptr727/dwspectrum/latest?label=latest&logo=docker
[hubdwspectrumlsio-link]: https://hub.docker.com/r/ptr727/dwspectrum-lsio
[hubdwspectrumlsiobeta-shield]: https://img.shields.io/docker/v/ptr727/dwspectrum-lsio/beta?label=beta&logo=docker
[hubdwspectrumlsiolatest-shield]: https://img.shields.io/docker/v/ptr727/dwspectrum-lsio/latest?label=latest&logo=docker
[hubdwspectrumlsiorc-shield]: https://img.shields.io/docker/v/ptr727/dwspectrum-lsio/rc?label=rc&logo=docker
[hubdwspectrumlsiostable-shield]: https://img.shields.io/docker/v/ptr727/dwspectrum-lsio/stable?label=stable&logo=docker
[hubdwspectrumrc-shield]: https://img.shields.io/docker/v/ptr727/dwspectrum/rc?label=rc&logo=docker
[hubdwspectrumstable-shield]: https://img.shields.io/docker/v/ptr727/dwspectrum/stable?label=stable&logo=docker
[hubnxgo-link]: https://hub.docker.com/r/ptr727/nxgo
[hubnxgobeta-shield]: https://img.shields.io/docker/v/ptr727/nxgo/beta?label=beta&logo=docker
[hubnxgolatest-shield]: https://img.shields.io/docker/v/ptr727/nxgo/latest?label=latest&logo=docker
[hubnxgolsio-link]: https://hub.docker.com/r/ptr727/nxgo-lsio
[hubnxgolsiobeta-shield]: https://img.shields.io/docker/v/ptr727/nxgo-lsio/beta?label=beta&logo=docker
[hubnxgolsiolatest-shield]: https://img.shields.io/docker/v/ptr727/nxgo-lsio/latest?label=latest&logo=docker
[hubnxgolsiorc-shield]: https://img.shields.io/docker/v/ptr727/nxgo-lsio/rc?label=rc&logo=docker
[hubnxgolsiostable-shield]: https://img.shields.io/docker/v/ptr727/nxgo-lsio/stable?label=stable&logo=docker
[hubnxgorc-shield]: https://img.shields.io/docker/v/ptr727/nxgo/rc?label=rc&logo=docker
[hubnxgostable-shield]: https://img.shields.io/docker/v/ptr727/nxgo/stable?label=stable&logo=docker
[hubnxmeta-link]: https://hub.docker.com/r/ptr727/nxmeta
[hubnxmetabeta-shield]: https://img.shields.io/docker/v/ptr727/nxmeta/beta?label=beta&logo=docker
[hubnxmetalatest-shield]: https://img.shields.io/docker/v/ptr727/nxmeta/latest?label=latest&logo=docker
[hubnxmetalsio-link]: https://hub.docker.com/r/ptr727/nxmeta-lsio
[hubnxmetalsiobeta-shield]: https://img.shields.io/docker/v/ptr727/nxmeta-lsio/beta?label=beta&logo=docker
[hubnxmetalsiolatest-shield]: https://img.shields.io/docker/v/ptr727/nxmeta-lsio/latest?label=latest&logo=docker
[hubnxmetalsiorc-shield]: https://img.shields.io/docker/v/ptr727/nxmeta-lsio/rc?label=rc&logo=docker
[hubnxmetalsiostable-shield]: https://img.shields.io/docker/v/ptr727/nxmeta-lsio/stable?label=stable&logo=docker
[hubnxmetarc-shield]: https://img.shields.io/docker/v/ptr727/nxmeta/rc?label=rc&logo=docker
[hubnxmetastable-shield]: https://img.shields.io/docker/v/ptr727/nxmeta/stable?label=stable&logo=docker
[hubnxwitness-link]: https://hub.docker.com/r/ptr727/nxwitness
[hubnxwitnessbeta-shield]: https://img.shields.io/docker/v/ptr727/nxwitness/beta?label=beta&logo=docker
[hubnxwitnesslatest-shield]: https://img.shields.io/docker/v/ptr727/nxwitness/latest?label=latest&logo=docker
[hubnxwitnesslsio-link]: https://hub.docker.com/r/ptr727/nxwitness-lsio
[hubnxwitnesslsiobeta-shield]: https://img.shields.io/docker/v/ptr727/nxwitness-lsio/beta?label=beta&logo=docker
[hubnxwitnesslsiolatest-shield]: https://img.shields.io/docker/v/ptr727/nxwitness-lsio/latest?label=latest&logo=docker
[hubnxwitnesslsiorc-shield]: https://img.shields.io/docker/v/ptr727/nxwitness-lsio/rc?label=rc&logo=docker
[hubnxwitnesslsiostable-shield]: https://img.shields.io/docker/v/ptr727/nxwitness-lsio/stable?label=stable&logo=docker
[hubnxwitnessrc-shield]: https://img.shields.io/docker/v/ptr727/nxwitness/rc?label=rc&logo=docker
[hubnxwitnessstable-shield]: https://img.shields.io/docker/v/ptr727/nxwitness/stable?label=stable&logo=docker
[hubwisenetwave-link]: https://hub.docker.com/r/ptr727/wisenetwave
[hubwisenetwavebeta-shield]: https://img.shields.io/docker/v/ptr727/wisenetwave/beta?label=beta&logo=docker
[hubwisenetwavelatest-shield]: https://img.shields.io/docker/v/ptr727/wisenetwave/latest?label=latest&logo=docker
[hubwisenetwavelsio-link]: https://hub.docker.com/r/ptr727/wisenetwave-lsio
[hubwisenetwavelsiobeta-shield]: https://img.shields.io/docker/v/ptr727/wisenetwave-lsio/beta?label=beta&logo=docker
[hubwisenetwavelsiolatest-shield]: https://img.shields.io/docker/v/ptr727/wisenetwave-lsio/latest?label=latest&logo=docker
[hubwisenetwavelsiorc-shield]: https://img.shields.io/docker/v/ptr727/wisenetwave-lsio/rc?label=rc&logo=docker
[hubwisenetwavelsiostable-shield]: https://img.shields.io/docker/v/ptr727/wisenetwave-lsio/stable?label=stable&logo=docker
[hubwisenetwaverc-shield]: https://img.shields.io/docker/v/ptr727/wisenetwave/rc?label=rc&logo=docker
[hubwisenetwavestable-shield]: https://img.shields.io/docker/v/ptr727/wisenetwave/stable?label=stable&logo=docker
[iniconfig-link]: https://meta.nxvms.com/docs/developers/knowledgebase/241-configuring-via-ini-files--iniconfig
[isbuildpublished-link]: https://github.com/networkoptix/nx_open/blob/526967920636d3119c92a5220290ecc10957bf12/vms/libs/nx_vms_update/src/nx/vms/update/releases_info.cpp#L31
[issues-link]: https://github.com/ptr727/NxWitness/issues
[lastbuild-shield]: https://byob.yarr.is/ptr727/NxWitness/lastbuild
[lastcommit-shield]: https://img.shields.io/github/last-commit/ptr727/NxWitness?logo=github&label=Last%20Commit
[license-link]: ./LICENSE
[license-shield]: https://img.shields.io/github/license/ptr727/NxWitness
[lsio-link]: https://www.linuxserver.io/
[lsiofleet-link]: https://fleet.linuxserver.io/
[lsiopuid-link]: https://docs.linuxserver.io/general/understanding-puid-and-pgid
[matrix-link]: https://docs.github.com/en/actions/using-jobs/using-a-matrix-for-your-jobs
[milestone-link]: https://doc.milestonesys.com/latest/en-US/standard_features/sf_mc/sf_systemoverview/mc_storageandarchivingexplained.htm
[networkoptix-link]: https://www.networkoptix.com/
[nxcloud-link]: https://www.networkoptix.com/nx-witness/nx-witness-cloud/
[nxcrunchbase-link]: https://www.crunchbase.com/organization/network-optix
[nxdebuglogging-link]: https://support.networkoptix.com/hc/en-us/articles/236033688-How-to-change-software-logging-level-and-how-to-get-logs
[nxdocker-link]: https://support.networkoptix.com/hc/en-us/articles/360037973573-Docker
[nxgithubcompose-link]: https://github.com/networkoptix/nxvms-docker/blob/master/docker-compose.yaml
[nxgithubdocker-link]: https://github.com/networkoptix/nxvms-docker
[nxgithubnetworking-link]: https://github.com/networkoptix/nxvms-docker#networking
[nxgithubstorage-link]: https://github.com/networkoptix/nxvms-docker#notes-about-storage
[nxgithubvolumes-link]: https://github.com/networkoptix/nxvms-docker#volumes-description
[nxgo-link]: https://updates.networkoptix.com/nxgo
[nxgobetadownload-link]: https://cloud.nxgo.io/download/betas/linux
[nxgodownload-link]: https://cloud.nxgo.io/download/releases/linux
[nxgoreleasenotes-link]: https://updates.networkoptix.com/nxgo/#releases_list
[nxgoreleases-link]: https://updates.networkoptix.com/nxgo/releases.json
[nxmarketplace-link]: https://www.networkoptix.com/nx-meta/nx-integrations-marketplace/
[nxmeta-link]: https://meta.nxvms.com/
[nxmetabetadownload-link]: https://meta.nxvms.com/downloads/betas
[nxmetadownload-link]: https://meta.nxvms.com/download/linux
[nxmetareleases-link]: https://updates.vmsproxy.com/metavms/releases.json
[nxossupport-link]: https://support.networkoptix.com/hc/en-us/articles/205313168-Nx-Witness-Operating-System-Support
[nxreleasenotes-link]: https://support.networkoptix.com/hc/en-us/articles/360042751193-Current-and-Past-Releases-Downloads-Release-Notes
[nxsupport-link]: https://support.networkoptix.com/hc/en-us/community/topics
[nxwebadmin-link]: https://support.networkoptix.com/hc/en-us/articles/115012831028-Nx-Server-Web-Admin
[nxwitness-link]: https://www.networkoptix.com/nx-witness/
[nxwitnessbetadownload-link]: https://beta.networkoptix.com/beta-builds/default
[nxwitnessdownload-link]: https://nxvms.com/download/linux
[nxwitnessreleasenotes-link]: https://www.networkoptix.com/all-nx-witness-release-notes
[nxwitnessreleases-link]: https://updates.vmsproxy.com/default/releases.json
[packages-link]: https://updates.networkoptix.com/default/38363/packages.json
[releasebuildstatus-shield]: https://img.shields.io/github/actions/workflow/status/ptr727/NxWitness/publish-release.yml?branch=main&logo=github&label=Build%20Status
[releaseinfo-link]: https://github.com/networkoptix/nx_open/blob/master/vms/libs/nx_vms_update/src/nx/vms/update/releases_info.cpp
[s6overlay-link]: https://github.com/just-containers/s6-overlay
[thehomegithub-link]: https://github.com/thehomerepot/nxwitness
[ubuntu-link]: https://ubuntu.com/
[ubuntudocker-link]: https://hub.docker.com/_/ubuntu
[ubuntulsiodocker-link]: https://hub.docker.com/r/lsiobase/ubuntu
[wisenetwavedownload-link]: https://wavevms.com/download/linux
[wisenetwavereleasenotes-link]: https://wavevms.com/release-notes/
[wisenetwavereleases-link]: https://updates.vmsproxy.com/hanwha/releases.json
