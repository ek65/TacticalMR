import os
import subprocess
import sys

def execute_command_on_files(directory):
    """
    Execute the command 'scenic {file} -S -b --count 1' on each file in a given directory.

    Args:
        directory (str): Path to the directory.
    """
    # Check if the provided path is a directory
    if not os.path.isdir(directory):
        print(f"{directory} is not a valid directory.")
        return

    # Get list of files in the directory
    files = sorted(os.listdir(directory))

    # Iterate over each file and execute the command
    for file in files:
        file_path = os.path.join(directory, file)
        if os.path.isfile(file_path):
            command = f'scenic "{file_path}" -S -b --count 1'
            try:
                print(f"Running '{command}' on {file_path}")
                result = subprocess.run(command, shell=True, check=True, capture_output=True, text=True)
                print(f"Output:\n{result.stdout}")
                if result.stderr:
                    print(f"Errors:\n{result.stderr}")
            except subprocess.CalledProcessError as e:
                print(f"Failed to run command '{command}' on {file_path}. Error: {e}")

def main():
    # Ensure the script takes in the directory as an argument
    if len(sys.argv) < 2:
        print("Usage: python script.py <directory>")
        sys.exit(1)

    directory = sys.argv[1]
    
    execute_command_on_files(directory)

if __name__ == "__main__":
    main()