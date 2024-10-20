import os
import sys
import shutil
files,dest = sys.argv[1:3] if len(sys.argv) >= 3 else ["",""]
#print(,dest)

for file in files.split(";"):
    if not os.path.exists(file):
        continue

    if os.path.isdir(file):
        dir = os.path.normpath(file)
        dest_dir = os.path.join(os.path.normpath(os.path.abspath(dest)),os.path.split(file)[1])
        print(f"Copying [{file}] to [{dest_dir}]")
        shutil.copytree(file,dest_dir,dirs_exist_ok=True)
    else:
        file = os.path.normpath(file)
        dest_dir = os.path.normpath(os.path.join(dest,file.split(os.path.sep)[-1]))
        print(f"Copying [{file}] to [{dest_dir}]")
        shutil.copy(file,dest_dir)
