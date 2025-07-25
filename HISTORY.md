# Docker Projects for Network Optix VMS Products

This is a project to build and publish docker images for various [Network Optix][networkoptix] VMS products.

## Release History

- Version 2.4:
  - Added [Hanwha Vision][hanwhavision] [Wisenet WAVE VMS][hanwhawave] builds, another US OEM whitelabel version Nx Witness.
  - Using the `CreateMatrix` utility instead of M4 to create Docker and Compose files for all product variants.
- Version 2.3:
  - Added unit test project to verify the release and upgrade control logic.
  - Switched from `Newtonsoft.Json` to .NET native `Text.Json`.
  - Modified builds to account for v6 Beta installers requiring the `file` package but not listing it in DEB `Depends`, see [#142](https://github.com/ptr727/NxWitness/issues/142).
- Version 2.2:
  - Simplified `Dockerfile` creation by using shell scripts instead of a `Makefile`.
- Version 2.1:
  - Added ARM64 images per user [request](https://github.com/ptr727/NxWitness/issues/131).
    - Note that testing was limited to verifying that the containers run on a Raspberry Pi 5.
  - Updated build scripts to use `docker compose` (vs. `docker-compose`) and `docker buildx` (vs. `docker build`) per current Docker/Moby v25+ [release](https://docs.docker.com/engine/install/).
  - Updated `CreateMatrix` tooling to use the newest version for the `latest` tag when multiple versions are available.
- Version 2.0:
  - Added a build release [version](./version.json), this version is independent of Nx release versions, and only identifies the version of the build environment, and is used in the image label.
  - Nx released v5.1 across all product brands, v5.1 [supports][nx_os_support] Ubuntu Jammy 22.04 LTS, and all base images have been updated to Jammy.
  - Due to the Jammy dependency versions older than v5.1 are no longer being built.
  - Build scripts removed support for old v4 variants.
  - Added a link from `/root/.config/nx_ini` to `/config/ini` for additional INI configuration files.

[hanwhavision]: https://hanwhavisionamerica.com/
[hanwhawave]: https://wavevms.com/
[networkoptix]: https://www.networkoptix.com/
[nx_os_support]: https://support.networkoptix.com/hc/en-us/articles/205313168-Nx-Witness-Operating-System-Support
