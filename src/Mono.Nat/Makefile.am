KEY_FILE=../mono-nat.snk
ASSEMBLY_COMPILER_COMMAND = gmcs
ASSEMBLY_COMPILER_FLAGS = -keyfile:$(KEY_FILE) -noconfig -codepage:utf8 -warn:4 -debug -d:DEBUG
ASSEMBLY_NAME=Mono.Nat
ASSEMBLY = $(MONO_NAT_BUILD_DIR)/$(ASSEMBLY_NAME).dll
COMPILE_TARGET = library
PC_FILES_IN = mono-nat.pc.in

REFERENCES =  \
	-r:System \
	-r:System.Xml

RESOURCES = 
EXTRAS = 
DATA_FILES = 

FILES =  \
	AbstractNatController.cs \
	AbstractNatDevice.cs \
	AssemblyInfo.cs \
	AsyncResults/AbstractPortMapAsyncResult.cs \
	Enums/MapState.cs \
	Enums/ProtocolType.cs \
	EventArgs/DeviceEventArgs.cs \
	Exceptions/MappingException.cs \
	INatController.cs \
	INatDevice.cs \
	Mapping.cs \
	NatUtility.cs \
	Pmp/AsyncResults/PortMapAsyncResult.cs \
	Pmp/PmpConstants.cs \
	Pmp/PmpNatController.cs \
	Pmp/PmpNatDevice.cs \
	TimerUtility.cs \
	Upnp/AsyncResults/GetAllMappingsAsyncResult.cs \
	Upnp/AsyncResults/PortMapAsyncResult.cs \
	Upnp/Messages/DiscoverDeviceMessage.cs \
	Upnp/Messages/ErrorMessage.cs \
	Upnp/Messages/GetServicesMessage.cs \
	Upnp/Messages/Requests/CreatePortMappingMessage.cs \
	Upnp/Messages/Requests/DeletePortMappingMessage.cs \
	Upnp/Messages/Requests/GetExternalIPAddressMessage.cs \
	Upnp/Messages/Requests/GetGenericPortMappingEntry.cs \
	Upnp/Messages/Requests/GetSpecificPortMappingEntryMessage.cs \
	Upnp/Messages/Responses/CreatePortMappingResponseMessage.cs \
	Upnp/Messages/Responses/DeletePortMappingResponseMessage.cs \
	Upnp/Messages/Responses/GetExternalIPAddressResponseMessage.cs \
	Upnp/Messages/Responses/GetGenericPortMappingEntryResponseMessage.cs \
	Upnp/Messages/UpnpMessage.cs \
	Upnp/UpnpNatController.cs \
	Upnp/UpnpNatDevice.cs

all: $(ASSEMBLY)

$(ASSEMBLY) $(ASSEMBLY).mdb: $(build_sources) $(build_resources) $(build_datafiles) 
	mkdir -p $(dir $(ASSEMBLY))		
	$(ASSEMBLY_COMPILER_COMMAND) $(ASSEMBLY_COMPILER_FLAGS) -out:$@ -target:$(COMPILE_TARGET) $(build_sources) $(build_resources_embed) $(build_references_ref)

install-data-local:
	echo "$(GACUTIL) /i $(ASSEMBLY) /f $(GACUTIL_FLAGS)";  \
	$(GACUTIL) /i $(ASSEMBLY) /f $(GACUTIL_FLAGS) || exit 1;

uninstall-local:
	echo "$(GACUTIL) /u $(ASSEMBLY_NAME) $(GACUTIL_FLAGS)"; \
	$(GACUTIL) /u $(ASSEMBLY_NAME) $(GACUTIL_FLAGS) || exit 1;

include $(top_srcdir)/Makefile.include
