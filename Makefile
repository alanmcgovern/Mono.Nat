CSC=gmcs 
OUT=Mono.Nat.dll
REFERENCES=System,System.Xml
SOURCES=AssemblyInfo.cs \
INatDevice.cs \
Mapping.cs \
MappingException.cs \
NatController.cs \
UpnpNatDevice.cs \
AsyncResults/PortMapAsyncResult.cs \
AsyncResults/GetAllMappingsAsyncResult.cs \
Enums/ProtocolType.cs \
Enums/MapState.cs \
EventArgs/DeviceEventArgs.cs \
Messages/uPnPMessages/DiscoverDeviceMessage.cs \
Messages/uPnPMessages/ErrorMessage.cs \
Messages/uPnPMessages/GetServicesMessage.cs \
Messages/uPnPMessages/UpnpMessage.cs \
Messages/uPnPMessages/Requests/CreatePortMappingMessage.cs \
Messages/uPnPMessages/Requests/DeletePortMappingMessage.cs \
Messages/uPnPMessages/Requests/GetExternalIPAddressMessage.cs \
Messages/uPnPMessages/Requests/GetGenericPortMappingEntry.cs \
Messages/uPnPMessages/Requests/GetSpecificPortMappingEntryMessage.cs \
Messages/uPnPMessages/Responses/CreatePortMappingResponseMessage.cs \
Messages/uPnPMessages/Responses/DeletePortMappingResponseMessage.cs \
Messages/uPnPMessages/Responses/GetGenericPortMappingEntryResponseMessage.cs \
Messages/uPnPMessages/Responses/GetExternalIPAddressResponseMessage.cs


all: Library

Library: $(SOURCES)
	$(CSC) -r:$(REFERENCES) -target:library -out:$(OUT) $(SOURCES)
