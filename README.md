# py-poetry
A template that has python configured for vs code with poetry.

## Running the Agent

To run the agent, use the following command:

```sh
python src/agent.py <path_to_local_git_repo>
```

Replace `<path_to_local_git_repo>` with the path to your local git repository.

## Using CodeQL with the Generated Docker Container

To use CodeQL queries with the generated docker container, follow these steps:

1. Ensure that the `codeql-config.yml` file is generated in the root directory of the docker container.
2. Build the docker container using the provided Dockerfile and docker-compose.yml files.
3. Run the docker container.
4. Use CodeQL CLI to run queries against the python files in the container.

For example, to run a CodeQL query, use the following command:

```sh
codeql database create my-database --language=python --source-root=/workspace
codeql database analyze my-database python-security-extended.qls --format=sarif-latest --output=results.sarif
```

Replace `my-database` with the desired name for your CodeQL database.
