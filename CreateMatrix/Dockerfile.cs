using System.Text;
using Serilog;

namespace CreateMatrix;

public class DockerFile
{
    public static void Create(
        List<ProductInfo> productList,
        string dockerPath,
        VersionInfo.LabelType label
    )
    {
        // Create a Docker file for each product type
        foreach (ProductInfo.ProductType productType in ProductInfo.GetProductTypes())
        {
            // Find the matching product
            ProductInfo? productInfo = productList.Find(item => item.Product == productType);
            ArgumentNullException.ThrowIfNull(productInfo);

            // Get the version for the label, not all releases include Beta and RC labels
            VersionInfo? versionInfo = productInfo.Versions.Find(item =>
                item.Labels.Contains(label)
            );

            // If the specific label is not found, use the latest version
            if (versionInfo == default(VersionInfo))
            {
                Log.Logger.Warning(
                    "Label {Label} not found for {Product}, using latest",
                    label,
                    productType
                );
                versionInfo = productInfo.Versions.Find(item =>
                    item.Labels.Contains(VersionInfo.LabelType.Latest)
                );
            }
            ArgumentNullException.ThrowIfNull(versionInfo);

            // Create the standard Docker file
            string dockerFile = CreateDockerFile(productType, versionInfo, false);
            string filePath = Path.Combine(
                dockerPath,
                $"{ProductInfo.GetDocker(productType, false)}.Dockerfile"
            );
            Log.Logger.Information("Writing Docker file to {Path}", filePath);
            File.WriteAllText(filePath, dockerFile);

            // Create the LSIO Docker file
            dockerFile = CreateDockerFile(productType, versionInfo, true);
            filePath = Path.Combine(
                dockerPath,
                $"{ProductInfo.GetDocker(productType, true)}.Dockerfile"
            );
            Log.Logger.Information("Writing Docker file to {Path}", filePath);
            File.WriteAllText(filePath, dockerFile);
        }
    }

    private static string CreateDockerFile(
        ProductInfo.ProductType productType,
        VersionInfo versionInfo,
        bool lsio
    )
    {
        // From
        StringBuilder stringBuilder = new();
        _ = stringBuilder.AppendLine(CreateFrom(productType, lsio));

        // Args
        _ = stringBuilder.AppendLine(CreateArgs(productType, versionInfo, lsio));

        // Install
        _ = stringBuilder.AppendLine(CreateInstall(lsio));

        // Entrypoint
        _ = stringBuilder.AppendLine(CreateEntryPoint(productType, lsio));

        return stringBuilder.ToString();
    }

    private static string CreateFrom(ProductInfo.ProductType productType, bool lsio)
    {
        string from = $$"""
            # Dockerfile created by CreateMatrix, do not modify by hand
            # Product: {{productType}}
            # Description: {{ProductInfo.GetDescription(productType)}}
            # Company: {{ProductInfo.GetCompany(productType)}}
            # Release: {{ProductInfo.GetRelease(productType)}}
            # LSIO: {{lsio}}

            # https://support.networkoptix.com/hc/en-us/articles/205313168-Nx-Witness-Operating-System-Support
            # Latest Ubuntu supported for v6 is Noble

            """;
        if (lsio)
        {
            from += """
                FROM lsiobase/ubuntu:noble

                """;
        }
        else
        {
            from += """
                FROM ubuntu:noble

                """;
        }
        return from;
    }

    private static string CreateArgs(
        ProductInfo.ProductType productType,
        VersionInfo versionInfo,
        bool lsio
    ) =>
            // Args
            $$"""
            # Labels
            ARG LABEL_NAME="{{ProductInfo.GetDocker(productType, lsio)}}"
            ARG LABEL_DESCRIPTION="{{ProductInfo.GetDescription(productType)}}"
            ARG LABEL_VERSION="{{versionInfo.Version}}"

            # Download URL and version
            # Current values are defined by the build pipeline
            ARG DOWNLOAD_X64_URL="{{versionInfo.UriX64}}"
            ARG DOWNLOAD_ARM64_URL="{{versionInfo.UriArm64}}"
            ARG DOWNLOAD_VERSION="{{versionInfo.Version}}"

            # Used for ${COMPANY_NAME} setting the server user and install directory
            ARG RUNTIME_NAME="{{ProductInfo.GetCompany(productType)}}"

            # Global builder variables
            # https://docs.docker.com/engine/reference/builder/#automatic-platform-args-in-the-global-scope
            ARG \
                # Platform of the build result. Eg linux/amd64, linux/arm/v7, windows/amd64
                TARGETPLATFORM \
                # Architecture component of TARGETPLATFORM
                TARGETARCH \
                # Platform of the node performing the build
                BUILDPLATFORM

            # The RUN wget command will be cached unless we change the cache tag
            # Use the download version for the cache tag
            ARG CACHE_DATE=${DOWNLOAD_VERSION}

            # Prevent EULA and confirmation prompts in installers
            ARG DEBIAN_FRONTEND=noninteractive

            # Media server user and directory name
            ENV COMPANY_NAME=${RUNTIME_NAME}

            # Labels
            LABEL name=${LABEL_NAME}-${DOWNLOAD_VERSION} \
                description=${LABEL_DESCRIPTION} \
                version=${LABEL_VERSION} \
                maintainer="Pieter Viljoen <ptr727@users.noreply.github.com>"

            """;

    private static string CreateInstall(bool lsio)
    {
        // Install
        string install = """
            # Install required tools and utilities
            RUN apt-get update \
                && apt-get upgrade --yes \
                && apt-get install --no-install-recommends --yes \
                    ca-certificates \
                    unzip \
                    wget

            # Download the installer file
            RUN mkdir -p /temp
            COPY download.sh /temp/download.sh
            # Set the working directory to /temp
            WORKDIR /temp
            RUN chmod +x download.sh \
                && ./download.sh


            """;
        if (lsio)
        {
            install += """
                # LSIO maps the host PUID and PGID environment variables to "abc" in the container.
                # https://docs.linuxserver.io/misc/non-root/
                # LSIO does not officially support changing the "abc" username
                # https://discourse.linuxserver.io/t/changing-abc-container-user/3208
                # https://github.com/linuxserver/docker-baseimage-ubuntu/blob/noble/root/etc/s6-overlay/s6-rc.d/init-adduser/run
                # The mediaserver calls "chown ${COMPANY_NAME}" at runtime
                # Change LSIO user "abc" to ${COMPANY_NAME}
                RUN usermod -l ${COMPANY_NAME} abc \
                # Change group "abc" to ${COMPANY_NAME}
                    && groupmod -n ${COMPANY_NAME} abc \
                # Replace "abc" with ${COMPANY_NAME}
                    && sed -i "s/abc/\${COMPANY_NAME}/g" /etc/s6-overlay/s6-rc.d/init-adduser/run


                """;
        }

        install += """
            # Install the mediaserver and dependencies
            RUN apt-get update \
                && apt-get install --no-install-recommends --yes \
                    gdb \

            """;
        if (!lsio)
        {
            install += """
                        sudo \

                """;
        }

        install += """
                    ./vms_server.deb \
            # Cleanup
                && apt-get clean \
                && apt-get autoremove --purge \
                && rm -rf /var/lib/apt/lists/* \
                && rm -rf /temp


            """;
        if (lsio)
        {
            install += """
                # Set ownership permissions
                RUN chown --verbose ${COMPANY_NAME}:${COMPANY_NAME} /opt/${COMPANY_NAME}/mediaserver/bin \
                    && chown --verbose ${COMPANY_NAME}:${COMPANY_NAME} /opt/${COMPANY_NAME}/mediaserver/bin/external.dat

                """;
        }
        else
        {
            install += """
                # Add the mediaserver ${COMPANY_NAME} user to the sudoers group
                # Only allow sudo no password access to the root-tool
                RUN echo "${COMPANY_NAME} ALL = NOPASSWD: /opt/${COMPANY_NAME}/mediaserver/bin/root-tool" > /etc/sudoers.d/${COMPANY_NAME}

                """;
        }

        return install;
    }

    private static string CreateEntryPoint(ProductInfo.ProductType productType, bool lsio) =>
        // Entrypoint
        lsio
            ? """
                # Copy etc init and services files
                # https://github.com/just-containers/s6-overlay#container-environment
                # https://www.linuxserver.io/blog/how-is-container-formed
                COPY s6-overlay /etc/s6-overlay

                # Expose port 7001
                EXPOSE 7001

                # Create mount points
                # Config links will be created at runtime, see LSIO/etc/s6-overlay/s6-rc.d/init-nx-relocate/run
                # /opt/${COMPANY_NAME}/mediaserver/etc -> /config/etc
                # /opt/${COMPANY_NAME}/mediaserver/var -> /config/var
                # /root/.config/nx_ini links -> /config/ini
                # /config is for configuration
                # /media is for recordings
                # /backup is for backups
                # /analytics is for analytics
                VOLUME /config /media /backup /analytics
                """
            : $$"""
                # Copy the entrypoint.sh launch script
                # entrypoint.sh will run the mediaserver and root-tool
                COPY entrypoint.sh /opt/entrypoint.sh
                RUN chmod +x /opt/entrypoint.sh

                # Run the entrypoint as the mediaserver ${COMPANY_NAME} user
                # Note that this user exists in the container and does not directly map to a user on the host
                USER ${COMPANY_NAME}

                # Runs entrypoint.sh on container start
                ENTRYPOINT ["/opt/entrypoint.sh"]

                # Expose port 7001
                EXPOSE 7001

                # Create mount points
                # Link config directly to internal paths
                # /mnt/config/etc:opt/{{ProductInfo.GetCompany(
                    productType
                ).ToLowerInvariant()}}/mediaserver/etc
                # /mnt/config/nx_ini:/home/{{ProductInfo.GetCompany(
                    productType
                ).ToLowerInvariant()}}/.config/nx_ini
                # /mnt/config/var:/opt/{{ProductInfo.GetCompany(
                    productType
                ).ToLowerInvariant()}}/mediaserver/var
                # /media is for recordings
                # /backup is for backups
                # /analytics is for analytics
                VOLUME /media /backup /analytics
                """;
}
