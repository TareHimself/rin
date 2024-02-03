#include <argparse/argparse.hpp>
#include <reflect/Generate.hpp>
#include <reflect/Parser.hpp>


void getAllHeadersInDir(std::vector<std::filesystem::path>& files,const std::filesystem::path& dir,const std::string& headerExt)
{
    for (auto & entry : std::filesystem::directory_iterator(dir))
    {
        auto ext = entry.path().extension();
        if(is_directory(entry.path()))
        {
            getAllHeadersInDir(files,entry.path(),headerExt);
        }
        else if(entry.path().extension() == "." + headerExt && !entry.path().string().ends_with("reflect" + entry.path().extension().string()))
        {
            files.push_back(entry.path());
        }
    }
}
int main(int argc, char** argv)
{
    argparse::ArgumentParser program("reflect");
    program.add_argument("-s","--source")
    .required()
    .help("The directory with the source files to reflect");

    program.add_argument("-o","--output")
    .help("The directory to output to");
    program.add_argument("-e","--ext")
    .help("The header extension")
    .required()
    .default_value("hpp");

    try
    {
        program.parse_args(argc,argv);
        std::filesystem::path sourceDir = program.get("-s");
        sourceDir = sourceDir.lexically_normal();
        std::filesystem::path outputDir = sourceDir;
        
        if(const auto val = program.present("-o"))
        {
            outputDir = val.value();
            outputDir = outputDir.lexically_normal();
        }
        
        reflect::parser::Parser parser;
        getAllHeadersInDir(parser.files,sourceDir,program.get("-e"));
        if(!parser.files.empty())
        {
            std::cout << "Preparing to parse " << parser.files.size() << " From " << sourceDir << std::endl;
            const auto parsedFiles = parser.Parse();
            std::cout << "Parsing Complete" << std::endl;
            if(!parsedFiles.empty())
            {
                std::cout << "Generating " << parsedFiles.size() << " Files to " << outputDir << std::endl;
                for (auto& f : parsedFiles)
                {
                    auto relativeToSourceDir = f->filePath.string().substr(sourceDir.string().size());
                    relativeToSourceDir = relativeToSourceDir.substr(0, relativeToSourceDir.size() - f->filePath.filename().string().size());
                    auto fileName = f->filePath.filename().string();
                    auto extension = f->filePath.extension().string();
                    std::filesystem::path newFilePath  = outputDir.string() + relativeToSourceDir + (fileName.substr(0,fileName.size() - extension.size()) + ".reflect" + extension);
                    if(!exists(newFilePath.parent_path()))
                    {
                        std::filesystem::create_directories(newFilePath.parent_path());
                    }

                    std::cout << "Generating " << newFilePath << std::endl;
                    reflect::generate::Generate(f,newFilePath);
                    std::cout << "Generated " << newFilePath << std::endl;
                }
            }
            else
            {
                std::cout << "No files with reflect macros found, exiting" << std::endl;
            }
        }
        else
        {
            std::cout << "No files found in " << sourceDir << std::endl;
        }
    }
    catch (const std::exception& err) {
        std::cerr << err.what() << std::endl;
        std::cerr << program;
        std::exit(1);
    }
    
    
    return 0;
}
