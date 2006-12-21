CSC=gmcs 
OUT=Mono.Nat.dll
REFERENCES=System,System.Xml
SOURCES=AssemblyInfo.cs \
Mapping.cs \
MappingException.cs \
NatController.cs \
NatDevice.cs \
AsyncResults/PortMapAsyncResult.cs \
Enums/ProtocolType.cs \
Messages/IMessage.cs \
Messages/uPnPMessages/AddMappingResponseMessage.cs \
Messages/uPnPMessages/CreatePortMappingMessage.cs \
Messages/uPnPMessages/DeletePortMappingMessage.cs \
Messages/uPnPMessages/DeletePortMappingResponseMessage.cs \
Messages/uPnPMessages/DiscoverDeviceMessage.cs \
Messages/uPnPMessages/ErrorMessage.cs \
Messages/uPnPMessages/GetServicesMessage.cs \
Messages/uPnPMessages/Message.cs

all: Library

Library: $(SOURCES)
	$(CSC) -r:$(REFERENCES) -target:library -out:$(OUT) $(SOURCES)
