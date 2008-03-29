
EXTRA_DIST =  

# Warning: This is an automatically generated file, do not edit!

if ENABLE_DEBUG_ANY_CPU
ASSEMBLY_COMPILER_COMMAND = gmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -optimize- -debug "-define:DEBUG;TRACE"

ASSEMBLY = bin/Debug/Mono.Nat.dll
ASSEMBLY_MDB = $(ASSEMBLY).mdb
COMPILE_TARGET = library
PROJECT_REFERENCES = 
BUILD_DIR = bin/Debug/


endif

if ENABLE_RELEASE_ANY_CPU
ASSEMBLY_COMPILER_COMMAND = gmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -optimize+ -debug -define:DEBUG "-define:TRACE"

ASSEMBLY = bin/Release/Mono.Nat.dll
ASSEMBLY_MDB = $(ASSEMBLY).mdb
COMPILE_TARGET = library
PROJECT_REFERENCES = 
BUILD_DIR = bin/Release/


endif

AL=al2
SATELLITE_ASSEMBLY_NAME=Mono.Nat.resources.dll

LINUX_PKGCONFIG = \
	$(MONO_NAT_PC)  


	
all: $(ASSEMBLY) $(LINUX_PKGCONFIG) 

FILES = \
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

DATA_FILES = 

RESOURCES = 

EXTRAS = \
	mono.nat.pc.in 

REFERENCES =  \
	System \
	System.Data \
	System.Xml

DLL_REFERENCES = 

CLEANFILES = $(LINUX_PKGCONFIG) 

include $(top_srcdir)/Makefile.include

MONO_NAT_PC = $(BUILD_DIR)/mono.nat.pc

$(eval $(call emit-deploy-wrapper,MONO_NAT_PC,mono.nat.pc))


$(build_xamlg_list): %.xaml.g.cs: %.xaml
	xamlg '$<'

$(build_resx_resources) : %.resources: %.resx
	resgen2 '$<' '$@'

$(ASSEMBLY) $(ASSEMBLY_MDB): $(build_sources) $(build_resources) $(build_datafiles) $(DLL_REFERENCES) $(PROJECT_REFERENCES) $(build_xamlg_list) $(build_satellite_assembly_list)
	mkdir -p $(dir $(ASSEMBLY))
	$(ASSEMBLY_COMPILER_COMMAND) $(ASSEMBLY_COMPILER_FLAGS) -out:$(ASSEMBLY) -target:$(COMPILE_TARGET) $(build_sources_embed) $(build_resources_embed) $(build_references_ref)
