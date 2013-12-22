
EXTRA_DIST =  

# Warning: This is an automatically generated file, do not edit!

if ENABLE_DEBUG_ANY_CPU
ASSEMBLY_COMPILER_COMMAND = gmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -optimize- -debug "-define:DEBUG;TRACE"

ASSEMBLY = bin/Debug/Mono.Nat.Test.exe
ASSEMBLY_MDB = $(ASSEMBLY).mdb
COMPILE_TARGET = exe
PROJECT_REFERENCES =  \
	../Mono.Nat/bin/Debug/Mono.Nat.dll
BUILD_DIR = bin/Debug/

MONO_NAT_DLL_MDB_SOURCE=../Mono.Nat/bin/Debug/Mono.Nat.dll.mdb
MONO_NAT_DLL_SOURCE=../Mono.Nat/bin/Debug/Mono.Nat.dll

endif

if ENABLE_RELEASE_ANY_CPU
ASSEMBLY_COMPILER_COMMAND = gmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -optimize+ -debug -define:DEBUG "-define:TRACE"

ASSEMBLY = bin/Release/Mono.Nat.Test.exe
ASSEMBLY_MDB = $(ASSEMBLY).mdb
COMPILE_TARGET = exe
PROJECT_REFERENCES =  \
	../Mono.Nat/bin/Release/Mono.Nat.dll
BUILD_DIR = bin/Release/

MONO_NAT_DLL_MDB_SOURCE=../Mono.Nat/bin/Release/Mono.Nat.dll.mdb
MONO_NAT_DLL_SOURCE=../Mono.Nat/bin/Release/Mono.Nat.dll

endif

AL=al2
SATELLITE_ASSEMBLY_NAME=Mono.Nat.Test.resources.dll

PROGRAMFILES = \
	$(MONO_NAT_DLL_MDB) \
	$(MONO_NAT_DLL)  

BINARIES = \
	$(MONO_NAT_TEST)  


	
all: $(ASSEMBLY) $(PROGRAMFILES) $(BINARIES) 

FILES = \
	AssemblyInfo.cs \
	Main.cs 

DATA_FILES = 

RESOURCES = 

EXTRAS = \
	mono.nat.test.in 

REFERENCES =  \
	System \
	System.Data \
	System.Xml

DLL_REFERENCES = 

CLEANFILES = $(PROGRAMFILES) $(BINARIES) 

include $(top_srcdir)/Makefile.include

MONO_NAT_DLL_MDB = $(BUILD_DIR)/Mono.Nat.dll.mdb
MONO_NAT_DLL = $(BUILD_DIR)/Mono.Nat.dll
MONO_NAT_TEST = $(BUILD_DIR)/mono.nat.test

$(eval $(call emit-deploy-target,MONO_NAT_DLL_MDB))
$(eval $(call emit-deploy-target,MONO_NAT_DLL))
$(eval $(call emit-deploy-wrapper,MONO_NAT_TEST,mono.nat.test,x))


$(build_xamlg_list): %.xaml.g.cs: %.xaml
	xamlg '$<'

$(build_resx_resources) : %.resources: %.resx
	resgen2 '$<' '$@'

$(ASSEMBLY) $(ASSEMBLY_MDB): $(build_sources) $(build_resources) $(build_datafiles) $(DLL_REFERENCES) $(PROJECT_REFERENCES) $(build_xamlg_list) $(build_satellite_assembly_list)
	mkdir -p $(dir $(ASSEMBLY))
	$(ASSEMBLY_COMPILER_COMMAND) $(ASSEMBLY_COMPILER_FLAGS) -out:$(ASSEMBLY) -target:$(COMPILE_TARGET) $(build_sources_embed) $(build_resources_embed) $(build_references_ref)
