import os
import subprocess
import sys
import docker.utils
import git
import docker
import codeql_config

###
# 1. Create a tool for semantic kernel that achieves the following:
# 2. Given a git repository path, pull the repo to the local drive.
# 3. Create a docker container that uses the devcontainer.json file in this project for the base image settings.
# 4. Copy the repo to the docker container build definition.
# 5. Copy the src/app folder to the docker container build definition.
# 6. Expose the necessary ports for hosting an MCP server.
# 7. Make the docker container run the agent.py script with the repo path as an argument on startup.
# 8. Write the dockerfile and docker-compose.yml files.
###

def clone_repo(repo_path, local_dir):
    if not git.Repo(repo_path).git_dir:
        raise ValueError(f"The path {repo_path} is not a valid git repository.")
    if not os.path.exists(local_dir):
        os.makedirs(local_dir)
    repo = git.Repo.clone_from(repo_path, local_dir)
    return repo

def create_dockerfile(local_dir):
    # TODO: Forward environment variables to the docker container
    ### Create a Dockerfile to run the mcp server
    dockerfile_content = f"""
    FROM mcr.microsoft.com/devcontainers/universal:2

    # Copy the repository
    COPY {local_dir} /workspace

    # Copy the src/app folder
    COPY src/app /workspace/src/app

    # Expose necessary ports
    EXPOSE 8080

    # Install CodeQL
    RUN apt-get update && apt-get install -y wget
    RUN wget -q -O codeql.tar.gz https://github.com/github/codeql-action/releases/latest/download/codeql-bundle-linux64.tar.gz
    RUN tar -xzf codeql.tar.gz -C /opt
    RUN ln -s /opt/codeql/codeql /usr/local/bin/codeql

    # Register CodeQL in the PATH
    ENV PATH="$PATH:/opt/codeql"

    # Run the agent.py script
    CMD ["python", "/workspace/src/app/agent.py", "{local_dir}"]
