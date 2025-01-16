import os

def create_codeql_config(repo_path):
    codeql_config_content = """
    name: "Python CodeQL Configuration"
    queries:
      - python-security-extended.qls
      - python-security-and-quality.qls
    paths:
      - {repo_path}
    """.format(repo_path=repo_path)

    config_file_path = os.path.join(repo_path, 'codeql-config.yml')
    with open(config_file_path, 'w') as f:
        f.write(codeql_config_content)
