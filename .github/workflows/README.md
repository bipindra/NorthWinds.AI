# GitHub Actions Workflows

## Docker Build and Push

The `docker-build-push.yml` workflow automatically builds and pushes Docker images to GitHub Container Registry (ghcr.io).

### Triggers

- **Push to master/main**: Builds and pushes image tagged with branch name and commit SHA
- **Push tags (v*)**: Builds and pushes versioned images (e.g., v1.0.0)
- **Pull Requests**: Builds image but does not push (for testing)
- **Manual dispatch**: Can be triggered manually from GitHub Actions tab

### Image Tags

The workflow automatically creates multiple tags:
- `latest` - Latest build from default branch
- `master` or `main` - Latest build from default branch
- `master-<sha>` - Specific commit SHA
- `v1.0.0` - Semantic version tags
- `v1.0` - Major.minor version
- `v1` - Major version

### Image Location

Images are pushed to: `ghcr.io/bipindra/northwinds.ai`

### Usage

Pull the image:
```bash
docker pull ghcr.io/bipindra/northwinds.ai:latest
```

Run the container:
```bash
docker run -p 8080:8080 \
  -e ConnectionStrings__NorthwindsDb="your-connection-string" \
  ghcr.io/bipindra/northwinds.ai:latest
```

### Permissions

The workflow uses `GITHUB_TOKEN` which is automatically provided by GitHub Actions. No additional secrets are required.

### Multi-Architecture Support

The workflow builds images for both:
- `linux/amd64` (Intel/AMD)
- `linux/arm64` (Apple Silicon, ARM servers)
