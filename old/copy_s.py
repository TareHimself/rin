import os
import sys
import shutil
files,dest = sys.argv[1:3] if len(sys.argv) >= 3 else ["",""]
#print(,dest)

for file in files.split(";"):
    if not os.path.exists(file):
        continue
    
    file = os.path.normpath(file)
    dest_dir = os.path.normpath(os.path.join(dest,file.split(os.path.sep)[-1]))
    print(f"Copying [{file}] to [{dest_dir}]")
    shutil.copy(file,dest_dir)
