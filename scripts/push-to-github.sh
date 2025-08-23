#!/bin/bash
# Script to push to GitHub after repository is created

echo "Pushing WitchCityRope to GitHub..."

# Check if remote exists
if git remote get-url origin > /dev/null 2>&1; then
    echo "Remote 'origin' already exists"
else
    echo "Please add your GitHub remote first:"
    echo "git remote add origin https://github.com/YOUR-USERNAME/WitchCityRope.git"
    exit 1
fi

# Rename branch to main if needed
CURRENT_BRANCH=$(git branch --show-current)
if [ "$CURRENT_BRANCH" = "master" ]; then
    echo "Renaming branch from master to main..."
    git branch -M main
fi

# Push to GitHub
echo "Pushing to GitHub..."
git push -u origin main

echo "Done! Your code is now on GitHub."
echo "View your repository at: https://github.com/YOUR-USERNAME/WitchCityRope"