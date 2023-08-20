// dllmain.cpp : Defines the entry point for the DLL application.
#include "pch.h"
#include <windows.h>
#include <mmdeviceapi.h>
#include <devicetopology.h>
#include <ks.h>

#define EXPORT comment(linker, "/EXPORT:" __FUNCTION__ "=" __FUNCDNAME__)

BOOL APIENTRY DllMain(HMODULE hModule,
	DWORD  ul_reason_for_call,
	LPVOID lpReserved
)
{
	switch (ul_reason_for_call)
	{
	case DLL_PROCESS_ATTACH:
	case DLL_THREAD_ATTACH:
	case DLL_THREAD_DETACH:
	case DLL_PROCESS_DETACH:
		break;
	}
	return TRUE;
}

HRESULT GetDeviceEnumerator(IMMDeviceEnumerator** result) {
	return CoCreateInstance(__uuidof(MMDeviceEnumerator), NULL, CLSCTX_INPROC_SERVER, __uuidof(IMMDeviceEnumerator), (void**)result);
}

HRESULT GetDeviceFromId(LPCWSTR id, IMMDevice** result) {
	HRESULT hr = S_OK;

	IMMDeviceEnumerator* deviceEnumerator;
	if (FAILED(hr = GetDeviceEnumerator(&deviceEnumerator)))
		return hr;

	if (FAILED(hr = deviceEnumerator->GetDevice(id, result)))
		return hr;

	deviceEnumerator->Release();

	return hr;
}

HRESULT WINAPI GetDevice(GUID containerId, LPWSTR* deviceId) {
#pragma EXPORT

	HRESULT hr = S_OK;

	IMMDeviceEnumerator* deviceEnumerator;
	if (FAILED(hr = GetDeviceEnumerator(&deviceEnumerator)))
		return hr;

	IMMDeviceCollection* devices;
	if (FAILED(hr = deviceEnumerator->EnumAudioEndpoints(eRender, DEVICE_STATE_ACTIVE | DEVICE_STATE_UNPLUGGED, &devices)))
		return hr;

	UINT count = 0;
	if (FAILED(hr = devices->GetCount(&count)))
		return hr;

	if (count <= 0)
		return E_INVALIDARG;

	for (int i = 0; i < count; i++)
	{
		IMMDevice* device;
		if (FAILED(hr = devices->Item(i, &device)))
			return hr;

		if (FAILED(hr = device->GetId(deviceId)))
			return hr;
	}

	return hr;
}

// SettingsHandlers_Devices.dll!BluetoothAudioProvider::ConnectEndpoint
HRESULT WINAPI ConnectBtEndpoint(LPCWSTR endpointId, BOOL connect) {
#pragma EXPORT

	HRESULT hr = S_OK;

	IMMDevice* device;
	if (FAILED(hr = GetDeviceFromId(endpointId, &device)))
		return hr;

	IDeviceTopology* topology;
	if (FAILED(hr = device->Activate(__uuidof(IDeviceTopology), CLSCTX_INPROC_SERVER, NULL, (void**)&topology)))
		return hr;

	IConnector* connector;
	if (FAILED(hr = topology->GetConnector(0, &connector)))
		return hr;

	IConnector* connector2;
	if (FAILED(hr = connector->GetConnectedTo(&connector2)))
		return hr;

	IPart* part;
	if (FAILED(hr = connector2->QueryInterface(&part)))
		return hr;

	IDeviceTopology* topology2;
	if (FAILED(hr = part->GetTopologyObject(&topology2)))
		return hr;

	LPWSTR deviceId2;
	if (FAILED(hr = topology2->GetDeviceId(&deviceId2)))
		return hr;

	IMMDevice* device2;
	if (FAILED(hr = GetDeviceFromId(deviceId2, &device2)))
		return hr;

	IKsControl* ksControl;
	if (FAILED(hr = device2->Activate(__uuidof(IKsControl), CLSCTX_INPROC_SERVER, NULL, (void**)&ksControl)))
		return hr;

	KSPROPERTY prop;
	prop.Set = KSPROPSETID_BtAudio;
	prop.Id = connect ? KSPROPERTY_ONESHOT_RECONNECT : KSPROPERTY_ONESHOT_DISCONNECT;
	prop.Flags = KSPROPERTY_TYPE_GET;

	ULONG bytesReturned;
	if (FAILED(hr = ksControl->KsProperty(&prop, 0x18u, NULL, NULL, &bytesReturned)))
		return hr;

	device->Release();

	return hr;
}

HRESULT WINAPI IsBtConnected(LPCWSTR deviceId, BOOL* result) {
#pragma EXPORT

	HRESULT hr = S_OK;

	IMMDevice* device;
	if (FAILED(hr = GetDeviceFromId(deviceId, &device)))
		return hr;

	IDeviceTopology* devTopology;
	if (FAILED(hr = device->Activate(__uuidof(IDeviceTopology), CLSCTX_INPROC_SERVER, NULL, (void**)&devTopology)))
		return hr;

	IConnector* connector;
	if (FAILED(hr = devTopology->GetConnector(0, &connector)))
		return hr;

	if (FAILED(hr = connector->IsConnected(result)))
		return hr;

	device->Release();

	return hr;
}
