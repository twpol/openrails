// Compile with the following call:
//   cl /LD msdmo.cpp /link /NODEFAULTLIB

bool __stdcall _DllMainCRTStartup(void *hinstDLL, unsigned long fdwReason, void *lpReserved)
{
    return true;
}

extern "C" __declspec(dllexport) void DMOUnregister(void)
{
}

extern "C" __declspec(dllexport) void DMORegister(void)
{
}

extern "C" __declspec(dllexport) void MoFreeMediaType(void)
{
}

extern "C" __declspec(dllexport) void MoCopyMediaType(void)
{
}
