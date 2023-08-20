__int64 __fastcall BluetoothAudioProvider::ConnectEndpoint(
    BluetoothAudioProvider* this,
    const unsigned __int16* endpoint,
    bool shouldConnect)
{
    BOOL v3; // esi
    int v5; // eax
    unsigned int v6; // ebx
    int v7; // eax
    int v8; // eax
    int v9; // eax
    int v10; // eax
    int v11; // eax
    int v12; // eax
    int v13; // eax
    int v14; // eax
    __int64 v15; // rdx
    IKsControl* v16; // rcx
    IMMDevice* v17; // rcx
    IDeviceTopology* v18; // rcx
    IPart* v19; // rcx
    IConnector* v20; // rcx
    IConnector* v21; // rcx
    IDeviceTopology* v22; // rcx
    IMMDevice* v23; // rcx
    int v25; // [rsp+20h] [rbp-39h]
    int v26; // [rsp+20h] [rbp-39h]
    IMMDevice* v27; // [rsp+40h] [rbp-19h] BYREF
    IDeviceTopology* v28; // [rsp+48h] [rbp-11h] BYREF
    IPart* part; // [rsp+50h] [rbp-9h] BYREF
    IConnector* connectedToConnector; // [rsp+58h] [rbp-1h] BYREF
    IConnector* connector; // [rsp+60h] [rbp+7h] BYREF
    IDeviceTopology* deviceTopology; // [rsp+68h] [rbp+Fh] BYREF
    IMMDevice* device; // [rsp+70h] [rbp+17h] BYREF
    LPVOID pv; // [rsp+78h] [rbp+1Fh] BYREF
    KSIDENTIFIER v35; // [rsp+80h] [rbp+27h] BYREF
    wil::details::in1diag3* retaddr; // [rsp+B8h] [rbp+5Fh]
    ULONG v37; // [rsp+C0h] [rbp+67h] BYREF
    IKsControl* iksControl; // [rsp+D8h] [rbp+7Fh] BYREF

    v3 = shouldConnect;
    device = 0i64;
    v5 = ((__int64(__fastcall*)(IMMDeviceEnumerator*, const unsigned __int16*, IMMDevice**))this->e->lpVtbl->GetDevice)(
        this->e,
        endpoint,
        &device);
    v6 = v5;
    if (v5 < 0)
    {
        wil::details::in1diag3::Return_Hr(
            retaddr,
            (void*)0x295,
            (unsigned int)"onecoreuap\\shell\\devicesflow\\devicesflowdeviceprovider\\base\\audioconnection.cpp",
            (const char*)(unsigned int)v5,
            v25);
        goto LABEL_39;
    }
    deviceTopology = 0i64;
    v7 = ((__int64(__fastcall*)(IMMDevice*, GUID*, __int64))device->lpVtbl->Activate)(
        device,
        &GUID_2a07407e_6497_4a18_9787_32f79bd0d98f,
        1i64);
    v6 = v7;
    if (v7 >= 0)
    {
        connector = 0i64;
        v8 = ((__int64(__fastcall*)(IDeviceTopology*, _QWORD, IConnector**))deviceTopology->lpVtbl->GetConnector)(
            deviceTopology,
            0i64,
            &connector);
        v6 = v8;
        if (v8 < 0)
        {
            wil::details::in1diag3::Return_Hr(
                retaddr,
                (void*)0x29B,
                (unsigned int)"onecoreuap\\shell\\devicesflow\\devicesflowdeviceprovider\\base\\audioconnection.cpp",
                (const char*)(unsigned int)v8,
                (int)&deviceTopology);
        LABEL_35:
            v21 = connector;
            if (connector)
            {
                connector = 0i64;
                ((void(__fastcall*)(IConnector*))v21->lpVtbl->Release)(v21);
            }
            goto LABEL_37;
        }
        connectedToConnector = 0i64;
        v9 = ((__int64(__fastcall*)(IConnector*, IConnector**))connector->lpVtbl->GetConnectedTo)(
            connector,
            &connectedToConnector);
        v6 = v9;
        if (v9 < 0)
        {
            wil::details::in1diag3::Return_Hr(
                retaddr,
                (void*)0x29E,
                (unsigned int)"onecoreuap\\shell\\devicesflow\\devicesflowdeviceprovider\\base\\audioconnection.cpp",
                (const char*)(unsigned int)v9,
                (int)&deviceTopology);
        LABEL_33:
            v20 = connectedToConnector;
            if (connectedToConnector)
            {
                connectedToConnector = 0i64;
                ((void(__fastcall*)(IConnector*))v20->lpVtbl->Release)(v20);
            }
            goto LABEL_35;
        }
        part = 0i64;
        v10 = ((__int64(__fastcall*)(IConnector*, GUID*, IPart**))connectedToConnector->lpVtbl->QueryInterface)(
            connectedToConnector,
            &GUID_ae2de0e4_5bca_4f2d_aa46_5d13f8fdb3a9,
            &part);
        v6 = v10;
        if (v10 < 0)
        {
            wil::details::in1diag3::Return_Hr(
                retaddr,
                (void*)0x2A1,
                (unsigned int)"onecoreuap\\shell\\devicesflow\\devicesflowdeviceprovider\\base\\audioconnection.cpp",
                (const char*)(unsigned int)v10,
                (int)&deviceTopology);
        LABEL_31:
            v19 = part;
            if (part)
            {
                part = 0i64;
                ((void(__fastcall*)(IPart*))v19->lpVtbl->Release)(v19);
            }
            goto LABEL_33;
        }
        v28 = 0i64;
        v11 = ((__int64(__fastcall*)(IPart*, IDeviceTopology**))part->lpVtbl->GetTopologyObject)(part, &v28);
        v6 = v11;
        if (v11 < 0)
        {
            wil::details::in1diag3::Return_Hr(
                retaddr,
                (void*)0x2A4,
                (unsigned int)"onecoreuap\\shell\\devicesflow\\devicesflowdeviceprovider\\base\\audioconnection.cpp",
                (const char*)(unsigned int)v11,
                (int)&deviceTopology);
        LABEL_29:
            v18 = v28;
            if (v28)
            {
                v28 = 0i64;
                ((void(__fastcall*)(IDeviceTopology*))v18->lpVtbl->Release)(v18);
            }
            goto LABEL_31;
        }
        pv = 0i64;
        v12 = ((__int64(__fastcall*)(IDeviceTopology*, LPVOID*))v28->lpVtbl->GetDeviceId)(v28, &pv);
        v6 = v12;
        if (v12 < 0)
        {
            wil::details::in1diag3::Return_Hr(
                retaddr,
                (void*)0x2A7,
                (unsigned int)"onecoreuap\\shell\\devicesflow\\devicesflowdeviceprovider\\base\\audioconnection.cpp",
                (const char*)(unsigned int)v12,
                (int)&deviceTopology);
        LABEL_27:
            if (pv)
                CoTaskMemFree(pv);
            goto LABEL_29;
        }
        v27 = 0i64;
        v13 = ((__int64(__fastcall*)(IMMDeviceEnumerator*, LPVOID, IMMDevice**))this->e->lpVtbl->GetDevice)(
            this->e,
            pv,
            &v27);
        v6 = v13;
        if (v13 < 0)
        {
            wil::details::in1diag3::Return_Hr(
                retaddr,
                (void*)0x2AA,
                (unsigned int)"onecoreuap\\shell\\devicesflow\\devicesflowdeviceprovider\\base\\audioconnection.cpp",
                (const char*)(unsigned int)v13,
                (int)&deviceTopology);
        LABEL_25:
            v17 = v27;
            if (v27)
            {
                v27 = 0i64;
                ((void(__fastcall*)(IMMDevice*))v17->lpVtbl->Release)(v17);
            }
            goto LABEL_27;
        }
        iksControl = 0i64;
        v14 = v27->lpVtbl->Activate(v27, &GUID_28f54685_06fd_11d2_b27a_00a0c9223196, 1u, 0i64, (void**)&iksControl);
        v6 = v14;
        if (v14 >= 0)
        {
            v35.Set = GUID_7fa06c40_b8f6_4c7e_8556_e8c33a12e54d;
            v35.Id = !v3;
            v35.Flags = 1;
            v14 = iksControl->lpVtbl->KsProperty(iksControl, &v35, 0x18u, 0i64, 0, &v37);
            v6 = v14;
            if (v14 >= 0)
            {
                v6 = 0;
            LABEL_23:
                v16 = iksControl;
                if (iksControl)
                {
                    iksControl = 0i64;
                    ((void(__fastcall*)(IKsControl*))v16->lpVtbl->Release)(v16);
                }
                goto LABEL_25;
            }
            v15 = 693i64;
        }
        else
        {
            v15 = 685i64;
        }
        wil::details::in1diag3::Return_Hr(
            retaddr,
            (void*)v15,
            (unsigned int)"onecoreuap\\shell\\devicesflow\\devicesflowdeviceprovider\\base\\audioconnection.cpp",
            (const char*)(unsigned int)v14,
            v26);
        goto LABEL_23;
    }
    wil::details::in1diag3::Return_Hr(
        retaddr,
        (void*)0x298,
        (unsigned int)"onecoreuap\\shell\\devicesflow\\devicesflowdeviceprovider\\base\\audioconnection.cpp",
        (const char*)(unsigned int)v7,
        (int)&deviceTopology);
LABEL_37:
    v22 = deviceTopology;
    if (deviceTopology)
    {
        deviceTopology = 0i64;
        ((void(__fastcall*)(IDeviceTopology*))v22->lpVtbl->Release)(v22);
    }
LABEL_39:
    v23 = device;
    if (device)
    {
        device = 0i64;
        ((void(__fastcall*)(IMMDevice*))v23->lpVtbl->Release)(v23);
    }
    return v6;
}