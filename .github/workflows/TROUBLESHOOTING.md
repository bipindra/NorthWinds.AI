# GitHub Actions Troubleshooting

## Workflow Not Triggering?

If the workflow is not building automatically, check the following:

### 1. Verify GitHub Actions is Enabled
- Go to your repository on GitHub
- Click **Settings** → **Actions** → **General**
- Ensure "Allow all actions and reusable workflows" is selected
- Under "Workflow permissions", ensure "Read and write permissions" is selected

### 2. Check Workflow File Location
- The workflow file must be in `.github/workflows/` directory
- The file must be committed to the `master` or `main` branch
- Verify the file exists: `https://github.com/bipindra/NorthWinds.AI/blob/master/.github/workflows/docker-build-push.yml`

### 3. Check Actions Tab
- Go to **Actions** tab in your GitHub repository
- Look for any failed or skipped workflow runs
- Check if workflows are being disabled by branch protection rules

### 4. Manual Trigger
- Go to **Actions** tab → **Build and Push Docker Image**
- Click **Run workflow** button to manually trigger it
- This will help verify the workflow syntax is correct

### 5. Check Repository Settings
- Ensure the repository is not archived
- Verify you have write access to the repository
- Check if there are any organization-level restrictions on Actions

### 6. Verify Workflow Syntax
- The workflow should trigger on:
  - Push to `master` or `main` branch
  - Push of tags starting with `v*`
  - Pull requests to `master` or `main`
  - Manual dispatch from Actions tab

### 7. Check Recent Pushes
- Ensure you're pushing to the `master` branch (not a different branch)
- Verify the workflow file was included in the commit
- Check git log: `git log --oneline --all -- .github/workflows/docker-build-push.yml`

## Still Not Working?

1. Try creating a test commit to trigger the workflow:
   ```bash
   git commit --allow-empty -m "Test: Trigger GitHub Actions"
   git push
   ```

2. Check the Actions tab for any error messages

3. Verify the workflow file is valid YAML (no syntax errors)

4. Ensure the Dockerfile exists and is in the repository root
