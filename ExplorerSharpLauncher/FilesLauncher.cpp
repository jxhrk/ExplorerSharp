// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

#include <iostream>
#include <algorithm>
#include <exdisp.h>
#include <iostream>
#include <objbase.h>
#include <propvarutil.h>
#include <shtypes.h>
#include <ShlObj_core.h>
#include <ShObjIdl_core.h>
#include <vector>
#include <wil/resource.h>

#include "OpenInFolder.h"

// Link additional libraries
#pragma comment(lib, "ole32.lib")
#pragma comment(lib, "Propsys.lib")
#pragma comment(lib, "user32.lib")
#pragma comment(lib, "uuid.lib")

constexpr auto ID_TIMEREXPIRED = 101;

LRESULT CALLBACK WindowProc(HWND hwnd, UINT uMsg, WPARAM wParam, LPARAM lParam);
bool OpenInExistingShellWindow(const TCHAR* folderPath);
void RunFileExplorer(const TCHAR* openDirectory);
size_t strifind(const std::wstring& strHaystack, const std::wstring& strNeedle);
bool comparei(std::wstring stringA, std::wstring stringB);

int WINAPI WinMain(HINSTANCE hInstance, HINSTANCE hPrevInstance, LPSTR lpCmdLine, int cmdShow)
{
	auto oleCleanup = wil::OleInitialize_failfast();

	//Uncomment to attach debugger
	//Sleep(10 * 1000);

	int numArgs = 0;
	LPWSTR* szArglist = CommandLineToArgvW(GetCommandLine(), &numArgs);
	WCHAR openDirectory[MAX_PATH];

	if (numArgs > 1 && wcsnlen(szArglist[1], 1))
	{
		swprintf(openDirectory, _countof(openDirectory) - 1, L"%s", szArglist[1]);
		std::wcout << openDirectory << std::endl;
	}
	else
	{
		return 0;
	}

	LocalFree(szArglist);


	//if (OpenInExistingShellWindow(openDirectory))
	//{
	//	return 0;
	//}

	WNDCLASSEX wcex = { };
	wcex.cbSize = sizeof(wcex);
	wcex.lpfnWndProc = WindowProc;
	wcex.cbWndExtra = sizeof(OpenInFolder*);
	wcex.hInstance = hInstance;
	wcex.lpszClassName = L"ExplorerSharpLauncher";
	RegisterClassEx(&wcex);

	OpenInFolder openInFolder;

	// Create the window.
	HWND hwnd = CreateWindowEx(
		0,
		L"ExplorerSharpLauncher",
		L"Files Launcher",
		0,
		0, 0, 0, 0,
		HWND_MESSAGE,
		NULL,
		hInstance,
		&openInFolder
	);

	if (hwnd == NULL)
	{
		return 0;
	}

	SetTimer(hwnd, ID_TIMEREXPIRED, 50, NULL);

	ShowWindow(hwnd, SW_SHOWNORMAL);

	MSG msg = { };
	while (GetMessage(&msg, NULL, 0, 0) > 0)
	{
		TranslateMessage(&msg);
		DispatchMessage(&msg);
	}

	auto item = openInFolder.GetResult();

	TCHAR args[1024];
	if (item.empty())
	{
		swprintf(args, _countof(args) - 1, L"\"%s\"", openDirectory);
	}
	else
	{
		swprintf(args, _countof(args) - 1, L"\"%s\" -select", item.c_str());
	}

	//wchar_t path[FILENAME_MAX] = { 0 };
	//GetModuleFileNameW(nullptr, path, FILENAME_MAX);
	//std::wstring path_to_exe = std::filesystem::path(path).parent_path().wstring();

	SHELLEXECUTEINFO ShExecInfo = { 0 };
	ShExecInfo.cbSize = sizeof(SHELLEXECUTEINFO);
	ShExecInfo.fMask = SEE_MASK_NOASYNC | SEE_MASK_FLAG_NO_UI;
	ShExecInfo.lpFile = L"ExplorerSharp.exe";
	ShExecInfo.lpParameters = args;
	ShExecInfo.lpDirectory = L"D:\\REPOS\\ExplorerSharp\\ExplorerSharp\\bin\\Debug\\net8.0";
	ShExecInfo.nShow = SW_SHOW;

	if (!ShellExecuteEx(&ShExecInfo))
	{
		std::wcout << L"Protocol error: " << GetLastError() << std::endl;
	}
	

	return 0;
}

LRESULT CALLBACK WindowProc(HWND hwnd, UINT uMsg, WPARAM wParam, LPARAM lParam)
{
	auto* pContainer = (OpenInFolder*)GetWindowLongPtr(hwnd, GWLP_USERDATA);

	switch (uMsg)
	{
	case WM_NCCREATE:
	{
		CREATESTRUCT* pCreate = reinterpret_cast<CREATESTRUCT*>(lParam);
		pContainer = reinterpret_cast<OpenInFolder*>(pCreate->lpCreateParams);

		if (!pContainer)
		{
			PostQuitMessage(0);
			return 0;
		}

		pContainer->SetWindow(hwnd);
		SetWindowLongPtr(hwnd, GWLP_USERDATA, (LONG_PTR)pContainer);
		break;
	}

	case WM_NCDESTROY:
		SetWindowLongPtr(hwnd, GWLP_USERDATA, 0);
		return 0;

	case WM_TIMER:
		switch (wParam)
		{
		case ID_TIMEREXPIRED:
			PostQuitMessage(0);
			return 0;
		}
		break;
	}

	 // Jump across to the member window function (will handle all requests).
	if (pContainer != nullptr)
		return pContainer->WindowProcedure(hwnd, uMsg, wParam, lParam);
	else
		return DefWindowProc(hwnd, uMsg, wParam, lParam);
}

size_t strifind(const std::wstring& strHaystack, const std::wstring& strNeedle)
{
	auto it = std::search(
		strHaystack.begin(), strHaystack.end(),
		strNeedle.begin(), strNeedle.end(),
		[](wchar_t ch1, wchar_t ch2) { return std::toupper(ch1) == std::toupper(ch2); }
	);

	return it != strHaystack.end() ? it - strHaystack.begin() : std::wstring::npos;
}

bool comparei(std::wstring stringA, std::wstring stringB)
{
	transform(stringA.begin(), stringA.end(), stringA.begin(), std::toupper);
	transform(stringB.begin(), stringB.end(), stringB.begin(), std::toupper);

	return (stringA == stringB);
}

void RunFileExplorer(const TCHAR* openDirectory)
{
	// Run explorer
	SHELLEXECUTEINFO ShExecInfo = { 0 };
	ShExecInfo.cbSize = sizeof(SHELLEXECUTEINFO);
	ShExecInfo.lpFile = L"explorer.exe";

	if (openDirectory != NULL)
	{
		TCHAR args[1024];
		swprintf(args, _countof(args) - 1, L"\"%s\"", openDirectory);
		ShExecInfo.lpParameters = args;
	}

	ShExecInfo.nShow = SW_SHOW;
	ShellExecuteEx(&ShExecInfo);
}

bool OpenInExistingShellWindow(const TCHAR* folderPath)
{
	std::wstring openDirectory(folderPath);
	bool mustOpenInExplorer = false;

	if (strifind(openDirectory, L"::{") == 0)
		openDirectory = L"shell:" + openDirectory;

	if (strifind(openDirectory, L"shell:") == 0)
	{
		std::vector<std::wstring> supportedShellFolders{
			L"shell:::{645FF040-5081-101B-9F08-00AA002F954E}",
			L"shell:::{5E5F29CE-E0A8-49D3-AF32-7A7BDC173478}",
			L"shell:::{20D04FE0-3AEA-1069-A2D8-08002B30309D}",
			L"shell:::{F02C1A0D-BE21-4350-88B0-7367FC96EF3C}",
			L"shell:::{208D2C60-3AEA-1069-A2D7-08002B30309D}",
			L"Shell:RecycleBinFolder", L"Shell:NetworkPlacesFolder", L"Shell:MyComputerFolder"
		};

		auto it = std::find_if(
			supportedShellFolders.begin(), supportedShellFolders.end(),
			[openDirectory](std::wstring it) { return comparei(it, openDirectory); }
		);

		mustOpenInExplorer = it == supportedShellFolders.end();
	}

	IShellItem* psi;
	PIDLIST_ABSOLUTE controlPanelCategoryViewPidl;

	if (!SUCCEEDED(SHCreateItemFromParsingName(L"::{26EE0668-A00A-44D7-9371-BEB064C98683}", NULL, IID_IShellItem, (void**)&psi)))
	{
		if (mustOpenInExplorer)
			RunFileExplorer(openDirectory.c_str());

		return mustOpenInExplorer;
	}

	if (!SUCCEEDED(SHGetIDListFromObject(psi, &controlPanelCategoryViewPidl)))
	{
		psi->Release();
		if (mustOpenInExplorer)
			RunFileExplorer(openDirectory.c_str());

		return mustOpenInExplorer;
	}

	psi->Release();

	PIDLIST_ABSOLUTE targetFolderPidl;

	if (!SUCCEEDED(SHCreateItemFromParsingName(openDirectory.c_str(), NULL, IID_IShellItem, (void**)&psi)))
	{
		if (mustOpenInExplorer)
			RunFileExplorer(openDirectory.c_str());

		return mustOpenInExplorer;
	}

	if (!SUCCEEDED(SHGetIDListFromObject(psi, &targetFolderPidl)))
	{
		psi->Release();
		if (mustOpenInExplorer)
			RunFileExplorer(openDirectory.c_str());

		return mustOpenInExplorer;
	}

	psi->Release();

	bool opened = false;
	IShellWindows* shellWindows;
	if (SUCCEEDED(CoCreateInstance(CLSID_ShellWindows, NULL, CLSCTX_LOCAL_SERVER, IID_IShellWindows, (void**)&shellWindows)))
	{
		VARIANT v;
		V_VT(&v) = VT_I4;
		IDispatch* item;

		for (V_I4(&v) = 0; SUCCEEDED(shellWindows->Item(v, &item)) && item != NULL; V_I4(&v)++)
		{
			IServiceProvider* serv;
			if (SUCCEEDED(item->QueryInterface(IID_IServiceProvider, (void**)&serv)))
			{
				IShellBrowser* shellBrowser;
				if (SUCCEEDED(serv->QueryService(SID_STopLevelBrowser, IID_IShellBrowser, (void**)&shellBrowser)))
				{
			//		IShellView* shellView;
			//		if (SUCCEEDED(shellBrowser->QueryActiveShellView(&shellView)))
			//		{
			//			IFolderView* folderView;
			//			if (SUCCEEDED(shellView->QueryInterface(IID_IFolderView, (void**)&folderView)))
			//			{
			//				IPersistFolder2* folder;
			//				if (SUCCEEDED(folderView->GetFolder(IID_IPersistFolder2, (void**)&folder)))
			//				{
			//					PIDLIST_ABSOLUTE folderPidl;
			//					if (SUCCEEDED(folder->GetCurFolder(&folderPidl)))
			//					{
			//						if (ILIsParent(folderPidl, targetFolderPidl, true) ||
			//							ILIsEqual(folderPidl, controlPanelCategoryViewPidl))
			//						{
										if (SUCCEEDED(shellBrowser->BrowseObject(targetFolderPidl, SBSP_SAMEBROWSER | SBSP_ABSOLUTE)))
										{
											opened = true;
											break;
										}
			//						}
			//						CoTaskMemFree(folderPidl);
			//					}
			//					folder->Release();
			//				}
			//				folderView->Release();
			//			}
			//			shellView->Release();
			//		}
					shellBrowser->Release();
				}
				serv->Release();
			}
			item->Release();
		}

		shellWindows->Release();
	}

	CoTaskMemFree(targetFolderPidl);
	CoTaskMemFree(controlPanelCategoryViewPidl);

	if (!opened && mustOpenInExplorer)
		RunFileExplorer(openDirectory.c_str());

	return opened || mustOpenInExplorer;
}
