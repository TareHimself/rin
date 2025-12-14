#include "platform.hpp"

#include <iostream>

#ifdef RIN_PLATFORM_WIN
#include <string>
#include <shlobj.h>
#include <combaseapi.h>
int platformGet()
{
    return static_cast<int>(EPlatform::Windows);
}

void platformInit()
{
    CoInitializeEx(nullptr,COINIT::COINIT_MULTITHREADED);
}

void platformShutdown()
{

}

void platformSelectFile(const char* title, bool multiple, const char* filter, PathReceivedCallback callback)
{
    IFileOpenDialog* fd;
    if(SUCCEEDED(CoCreateInstance(CLSID_FileOpenDialog,nullptr,CLSCTX_INPROC_SERVER,IID_PPV_ARGS(&fd))))
    {
        DWORD options;

        if(multiple && SUCCEEDED(fd->GetOptions(&options)))
        {
            fd->SetOptions(options | FOS_ALLOWMULTISELECT);
        }
        std::string sTitle(title);
        fd->SetTitle(std::wstring(sTitle.begin(),sTitle.end()).c_str());

        std::string sFilter(filter);

        if(!sFilter.empty())
        {
            const std::wstring extensions(sFilter.begin(),sFilter.end());

            COMDLG_FILTERSPEC rgSpec[] =
            {
                {L"Filter", extensions.c_str()},
                {L"All", L"*.*"},
            };
            fd->SetFileTypes(ARRAYSIZE(rgSpec),rgSpec);
        }
        else
        {
            COMDLG_FILTERSPEC rgSpec[] =
            {
                {L"All", L"*.*"},
            };
            fd->SetFileTypes(ARRAYSIZE(rgSpec),rgSpec);
        }

        if(SUCCEEDED(fd->Show(NULL)))
        {

            if(multiple)
            {
                IShellItemArray* sia;
                if(SUCCEEDED(fd->GetResults(&sia)))
                {
                    DWORD count = 0;
                    if(SUCCEEDED(sia->GetCount(&count)))
                    {
                        for(DWORD i = 0; i < count; i++)
                        {
                            IShellItem* si;
                            if(SUCCEEDED(sia->GetItemAt(i,&si)))
                            {
                                PWSTR filePath = nullptr;

                                if(SUCCEEDED(si->GetDisplayName(SIGDN_DESKTOPABSOLUTEPARSING,&filePath)))
                                {
                                    std::wstring wstr(filePath);
                                    const int sizeNeeded = WideCharToMultiByte(CP_UTF8,0,wstr.data(),static_cast<int>(wstr.size()),nullptr,0,nullptr,nullptr);
                                    std::string strTo(sizeNeeded,0);
                                    WideCharToMultiByte(CP_UTF8,0,wstr.data(),static_cast<int>(wstr.size()),
                                                        strTo.data(),sizeNeeded,nullptr,nullptr);
                                    callback(strTo.c_str());
                                    CoTaskMemFree(filePath);
                                }

                                si->Release();
                            }
                        }
                    }
                    sia->Release();
                }
            }
            else
            {
                IShellItem* si;
                if(SUCCEEDED(fd->GetResult(&si)))
                {
                    PWSTR filePath = nullptr;

                    if(SUCCEEDED(si->GetDisplayName(SIGDN_DESKTOPABSOLUTEPARSING,&filePath)))
                    {
                        const std::wstring wstr(filePath);
                        const int sizeNeeded = WideCharToMultiByte(CP_UTF8,0,wstr.data(),static_cast<int>(wstr.size()),nullptr,0,nullptr,nullptr);
                        std::string strTo(sizeNeeded,0);
                        WideCharToMultiByte(CP_UTF8,0,wstr.data(),static_cast<int>(wstr.size()),
                                            strTo.data(),sizeNeeded,nullptr,nullptr);
                        callback(strTo.c_str());
                        CoTaskMemFree(filePath);
                    }

                    si->Release();
                }

            }

        }

        fd->Release();
    }
}

void platformSelectPath(const char* title, bool multiple, PathReceivedCallback callback)
{
    IFileOpenDialog* fd;
    if(SUCCEEDED(CoCreateInstance(CLSID_FileOpenDialog,NULL,CLSCTX_INPROC_SERVER,IID_PPV_ARGS(&fd))))
    {
        DWORD options;

        if(multiple && SUCCEEDED(fd->GetOptions(&options)))
        {
            fd->SetOptions(options | FOS_ALLOWMULTISELECT | FOS_PICKFOLDERS);
        }
        else if(SUCCEEDED(fd->GetOptions(&options)))
        {
            fd->SetOptions(options | FOS_PICKFOLDERS);
        }

        std::string sTitle(title);
        fd->SetTitle(std::wstring(sTitle.begin(),sTitle.end()).c_str());

        if(SUCCEEDED(fd->Show(NULL)))
        {

            if(multiple)
            {
                IShellItemArray* sia;
                if(SUCCEEDED(fd->GetResults(&sia)))
                {
                    DWORD count = 0;
                    if(SUCCEEDED(sia->GetCount(&count)))
                    {
                        for(DWORD i = 0; i < count; i++)
                        {
                            IShellItem* si;
                            if(SUCCEEDED(sia->GetItemAt(i,&si)))
                            {
                                PWSTR filePath = nullptr;

                                if(SUCCEEDED(si->GetDisplayName(SIGDN_DESKTOPABSOLUTEPARSING,&filePath)))
                                {
                                    const std::wstring wstr(filePath);
                                    const int sizeNeeded = WideCharToMultiByte(CP_UTF8,0,wstr.data(),static_cast<int>(wstr.size()),nullptr,0,nullptr,nullptr);
                                    std::string strTo(sizeNeeded,0);
                                    WideCharToMultiByte(CP_UTF8,0,wstr.data(),static_cast<int>(wstr.size()),
                                                        strTo.data(),sizeNeeded,nullptr,nullptr);
                                    callback(strTo.c_str());
                                    CoTaskMemFree(filePath);
                                }

                                si->Release();
                            }
                        }
                    }
                    sia->Release();
                }
            }
            else
            {
                IShellItem* si;
                if(SUCCEEDED(fd->GetResult(&si)))
                {
                    PWSTR filePath = nullptr;

                    if(SUCCEEDED(si->GetDisplayName(SIGDN_DESKTOPABSOLUTEPARSING,&filePath)))
                    {
                        const std::wstring wstr(filePath);
                        const int sizeNeeded = WideCharToMultiByte(CP_UTF8,0,wstr.data(),static_cast<int>(wstr.size()),nullptr,0,nullptr,nullptr);
                        std::string strTo(sizeNeeded,0);
                        WideCharToMultiByte(CP_UTF8,0,wstr.data(),static_cast<int>(wstr.size()),
                                            strTo.data(),sizeNeeded,nullptr,nullptr);
                        callback(strTo.c_str());
                        CoTaskMemFree(filePath);
                    }

                    si->Release();
                }

            }

        }

        fd->Release();
    }
}


#endif


#ifdef RIN_PLATFORM_LINUX
#include <xkbcommon/xkbcommon.h>
#include <libdecor.h>
#include <list>
#include <ranges>
#include <sstream>
#include <unistd.h>
#include <unordered_map>
#include <unordered_set>
#include <wayland-client.h>
#include <xdg-shell-client-protocol.h>
#include <linux/input-event-codes.h>
#include <sys/mman.h>

void platformInit() {
}

void platformShutdown() {

}

int platformGet() {
    return static_cast<int>(EPlatform::Linux);
}

static void runZenityCommand(std::ostringstream& command, bool multiple,const char* filter, PathReceivedCallback callback) {
    if (filter) {
        std::string filterString{filter};
        if (!filterString.empty()) {
            std::stringstream filterStream{filter};
            std::string parsedFilter;
            command << " --file-filter=\"";

            while (std::getline(filterStream,parsedFilter,';')) {
                if (!parsedFilter.empty()) {
                    command << " " << parsedFilter;
                }
            }

            command << "\"";
        }
    }

    command << " 2> /dev/null";

    auto cmd = command.str();
    FILE *pipe = popen(command.str().c_str(), "r");
    if (!pipe) return;

    std::string result;
    char buffer[4096];
    while (fgets(buffer, sizeof(buffer), pipe)) {
        result += buffer;
    }
    pclose(pipe);

    // Remove trailing newline
    if (!result.empty() && result.back() == '\n')
        result.pop_back();

    if (multiple) {
        std::stringstream ss(result);
        std::string path;
        while (std::getline(ss, path, ':')) {
            if (!path.empty())
                callback(path.c_str());
        }
    } else {
        if (!result.empty())
            callback(result.c_str());
    }
}

void platformSelectFile(const char *title, bool multiple, const char *filter,
                                    PathReceivedCallback callback) {
    std::ostringstream cmd;
    cmd << "zenity --file-selection";
    if (multiple)
        cmd << " --multiple --separator=\":\"";
    if (title)
        cmd << " --title=\"" << title << "\"";

    runZenityCommand(cmd, multiple,filter, callback);
}

void platformSelectPath(const char *title, bool multiple, PathReceivedCallback callback) {
    std::ostringstream cmd;
    cmd << "zenity --file-selection --directory";
    if (multiple)
        cmd << " --multiple --separator=\":\"";
    if (title)
        cmd << " --title=\"" << title << "\"";

    runZenityCommand(cmd, multiple,nullptr, callback);
}

#endif


#ifdef RIN_PLATFORM_MAC

void platformInit()
{

}
int platformGet()
{
    return static_cast<int>(EPlatform::Mac);
}

void platformInit()
{

}
int platformGet()
{
    return static_cast<int>(EPlatform::Linux);
}
void platformSelectFile(const char* title, bool multiple, const char* filter, PathReceivedCallback callback)
{

}
void platformSelectPath(const char* title, bool multiple, PathReceivedCallback callback)
{

}
#endif
