#include "rin/core/platform.hpp"
#include <string>
#include <stdexcept>

#ifdef RIN_PLATFORM_WINDOWS
#include <shlobj.h>
#include <combaseapi.h>
#include <stringapiset.h>
#endif
namespace rin::platform
{
    void init()
    {
#ifdef RIN_PLATFORM_WINDOWS
        CoInitializeEx(nullptr, COINIT_MULTITHREADED);
#endif
    }

    Platform get()
    {
#ifdef RIN_PLATFORM_WINDOWS
        return Platform::Windows;
#endif

#ifdef RIN_PLATFORM_APPLE
        return Platform::Apple;
#endif

#ifdef RIN_PLATFORM_LINUX
        return Platform::Linux;
#endif
    }

    std::vector<std::string> selectFile(const std::string& title, const bool multiple, const std::string& filter)
    {
        std::vector<std::string> results{};
#ifdef RIN_PLATFORM_WINDOWS
        IFileOpenDialog* fd;
        if (SUCCEEDED(CoCreateInstance(CLSID_FileOpenDialog,NULL,CLSCTX_INPROC_SERVER,IID_PPV_ARGS(&fd))))
        {
            DWORD options;

            if (multiple && SUCCEEDED(fd->GetOptions(&options)))
            {
                fd->SetOptions(options | FOS_ALLOWMULTISELECT);
            }
            std::string sTitle(title);
            fd->SetTitle(std::wstring(sTitle.begin(), sTitle.end()).c_str());

            std::string sFilter(filter);

            if (!sFilter.empty())
            {
                const std::wstring extensions(sFilter.begin(), sFilter.end());

                COMDLG_FILTERSPEC rgSpec[] =
                {
                    {L"Filter", extensions.c_str()},
                    {L"All", L"*.*"},
                };
                fd->SetFileTypes(ARRAYSIZE(rgSpec), rgSpec);
            }
            else
            {
                COMDLG_FILTERSPEC rgSpec[] =
                {
                    {L"All", L"*.*"},
                };
                fd->SetFileTypes(ARRAYSIZE(rgSpec), rgSpec);
            }

            if (SUCCEEDED(fd->Show(NULL)))
            {
                if (multiple)
                {
                    IShellItemArray* sia;
                    if (SUCCEEDED(fd->GetResults(&sia)))
                    {
                        DWORD count = 0;
                        if (SUCCEEDED(sia->GetCount(&count)))
                        {
                            for (DWORD i = 0; i < count; i++)
                            {
                                IShellItem* si;
                                if (SUCCEEDED(sia->GetItemAt(i,&si)))
                                {
                                    PWSTR filePath = nullptr;

                                    if (SUCCEEDED(si->GetDisplayName(SIGDN_DESKTOPABSOLUTEPARSING,&filePath)))
                                    {
                                        std::wstring wstr(filePath);
                                        const int sizeNeeded = WideCharToMultiByte(
                                            CP_UTF8, 0, wstr.data(), static_cast<int>(wstr.size()), nullptr, 0, nullptr,
                                            nullptr);
                                        std::string strTo(sizeNeeded, 0);
                                        WideCharToMultiByte(CP_UTF8, 0, wstr.data(), static_cast<int>(wstr.size()),
                                                            strTo.data(), sizeNeeded, nullptr, nullptr);
                                        results.push_back(strTo);
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
                    if (SUCCEEDED(fd->GetResult(&si)))
                    {
                        PWSTR filePath = nullptr;

                        if (SUCCEEDED(si->GetDisplayName(SIGDN_DESKTOPABSOLUTEPARSING,&filePath)))
                        {
                            const std::wstring wstr(filePath);
                            const int sizeNeeded = WideCharToMultiByte(
                                CP_UTF8, 0, wstr.data(), static_cast<int>(wstr.size()), nullptr, 0, nullptr, nullptr);
                            std::string strTo(sizeNeeded, 0);
                            WideCharToMultiByte(CP_UTF8, 0, wstr.data(), static_cast<int>(wstr.size()),
                                                strTo.data(), sizeNeeded, nullptr, nullptr);
                            results.push_back(strTo);
                            CoTaskMemFree(filePath);
                        }

                        si->Release();
                    }
                }
            }

            fd->Release();
        }
#endif
#ifdef RIN_PLATFORM_APPLE
        throw std::runtime_error("Not Implemented");
#endif
#ifdef RIN_PLATFORM_LINUX
        throw std::runtime_error("Not Implemented");
#endif


        return results;
    }

    std::vector<std::string> selectPath(const std::string& title, const bool multiple)
    {
        std::vector<std::string> results{};
#ifdef RIN_PLATFORM_WINDOWS
        IFileOpenDialog* fd;
        if (SUCCEEDED(CoCreateInstance(CLSID_FileOpenDialog,NULL,CLSCTX_INPROC_SERVER,IID_PPV_ARGS(&fd))))
        {
            DWORD options;

            if (multiple && SUCCEEDED(fd->GetOptions(&options)))
            {
                fd->SetOptions(options | FOS_ALLOWMULTISELECT | FOS_PICKFOLDERS);
            }
            else if (SUCCEEDED(fd->GetOptions(&options)))
            {
                fd->SetOptions(options | FOS_PICKFOLDERS);
            }

            std::string sTitle(title);
            fd->SetTitle(std::wstring(sTitle.begin(), sTitle.end()).c_str());

            if (SUCCEEDED(fd->Show(NULL)))
            {
                if (multiple)
                {
                    IShellItemArray* sia;
                    if (SUCCEEDED(fd->GetResults(&sia)))
                    {
                        DWORD count = 0;
                        if (SUCCEEDED(sia->GetCount(&count)))
                        {
                            for (DWORD i = 0; i < count; i++)
                            {
                                IShellItem* si;
                                if (SUCCEEDED(sia->GetItemAt(i,&si)))
                                {
                                    PWSTR filePath = nullptr;

                                    if (SUCCEEDED(si->GetDisplayName(SIGDN_DESKTOPABSOLUTEPARSING,&filePath)))
                                    {
                                        const std::wstring wstr(filePath);
                                        const int sizeNeeded = WideCharToMultiByte(
                                            CP_UTF8, 0, wstr.data(), static_cast<int>(wstr.size()), nullptr, 0, nullptr,
                                            nullptr);
                                        std::string strTo(sizeNeeded, 0);
                                        WideCharToMultiByte(CP_UTF8, 0, wstr.data(), static_cast<int>(wstr.size()),
                                                            strTo.data(), sizeNeeded, nullptr, nullptr);
                                        results.push_back(strTo);
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
                    if (SUCCEEDED(fd->GetResult(&si)))
                    {
                        PWSTR filePath = nullptr;

                        if (SUCCEEDED(si->GetDisplayName(SIGDN_DESKTOPABSOLUTEPARSING,&filePath)))
                        {
                            const std::wstring wstr(filePath);
                            const int sizeNeeded = WideCharToMultiByte(
                                CP_UTF8, 0, wstr.data(), static_cast<int>(wstr.size()), nullptr, 0, nullptr, nullptr);
                            std::string strTo(sizeNeeded, 0);
                            WideCharToMultiByte(CP_UTF8, 0, wstr.data(), static_cast<int>(wstr.size()),
                                                strTo.data(), sizeNeeded, nullptr, nullptr);
                            results.push_back(strTo);
                            CoTaskMemFree(filePath);
                        }

                        si->Release();
                    }
                }
            }

            fd->Release();
        }
#endif
#ifdef RIN_PLATFORM_APPLE
        throw std::runtime_error("Not Implemented");
#endif
#ifdef RIN_PLATFORM_LINUX
        throw std::runtime_error("Not Implemented");
#endif
        return results;
    }

    std::filesystem::path getExecutablePath()
    {
#ifdef RIN_PLATFORM_WINDOWS
        WCHAR path[MAX_PATH];
        GetModuleFileNameW(nullptr, path, MAX_PATH);
        const std::wstring wstr(path);
        const int sizeNeeded = WideCharToMultiByte(
            CP_UTF8, 0, wstr.data(), static_cast<int>(wstr.size()), nullptr, 0, nullptr,
            nullptr);
        std::string strTo(sizeNeeded, 0);
        WideCharToMultiByte(CP_UTF8, 0, wstr.data(), static_cast<int>(wstr.size()),
                            strTo.data(), sizeNeeded, nullptr, nullptr);
        return strTo;
#endif
#ifdef RIN_PLATFORM_APPLE
        throw std::runtime_error("Not Implemented");
#endif
#ifdef RIN_PLATFORM_LINUX
        throw std::runtime_error("Not Implemented");
#endif
    }
}
