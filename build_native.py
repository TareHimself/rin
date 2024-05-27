import os
import subprocess

cwd = os.path.join(os.getcwd(),"Native")
configuration = "Debug"
build_dir = os.path.join(os.path.join(cwd,"build"),configuration)

with open("foo.txt",'w') as f:
    f.write(cwd)
    f.write(build_dir)

with open(os.path.join(os.getcwd(),"Native","build.log"), "w+") as outfile:
    subprocess.run(f"cmake -DCMAKE_BUILD_TYPE={configuration} -S {cwd}\\ -B {build_dir}", stdout=outfile)
    subprocess.run(f"cmake --build {build_dir}\"", stdout=outfile)


