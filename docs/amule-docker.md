# aMule download client for eD2k/Kad

This fork adds experimental eD2k/Kad support through an external `amuled` instance.
Sonarr does not run aMule itself and does not shell out to `amulecmd`; it talks to
aMule through the External Connections protocol on TCP port `4712`.

## Docker compose example

`ngosang/amule` is a reference image for testing, not a hard dependency.

```yaml
services:
  amule:
    image: ngosang/amule:latest
    container_name: amule
    ports:
      - "4712:4712"      # External Connections, required by Sonarr
      - "4711:4711"      # Web UI, optional
      - "4662:4662/tcp"  # eD2k TCP
      - "4665:4665/udp"  # eD2k global search UDP
      - "4672:4672/udp"  # Kad/eMule UDP
    volumes:
      - ./amule-config:/home/amule/.aMule
      - ./downloads:/incoming
```

Configure `amuled` with External Connections enabled, a non-empty EC password,
and port `4712` reachable from Sonarr.

## Path mapping

The aMule Incoming path and the path Sonarr can read must resolve to the same
files. Either mount the same host path at the same container path in both
containers, or configure Sonarr remote path mapping for the aMule host.

If `4712` or the EC password is wrong, searching and adding downloads will fail.
If the eD2k/Kad ports are closed, aMule may still run but searches/downloads can
perform poorly or get Low ID.
