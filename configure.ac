AC_PREREQ(2.54)
AC_INIT([mono-nat], 0.6.0)
AM_INIT_AUTOMAKE([foreign])

AC_PATH_PROG(PKG_CONFIG, pkg-config, no)
if test "x$PKG_CONFIG" = "xno"; then
        AC_MSG_ERROR([You need to install pkg-config])
fi

AC_PROG_INSTALL

AC_PATH_PROG(GMCS, gmcs, no)
if test "x$GMCS" = "xno"; then
        AC_MSG_ERROR([gmcs Not found])
fi

AC_PATH_PROG(GACUTIL, gacutil, no)
if test "x$GACUTIL" = "xno" ; then
	AC_MSG_ERROR([No gacutil tool found])
fi
GACUTIL_FLAGS='/package mono-nat /root $(DESTDIR)$(prefix)/lib'

AC_SUBST(GACUTIL)
AC_SUBST(GACUTIL_FLAGS)

AC_SUBST(prefix)

MONO_NAT_BUILD_DIR=`pwd`/build
MONO_NAT_LIB='Mono.Nat.dll'

AC_SUBST(MONO_NAT_BUILD_DIR)
AC_SUBST(MONO_NAT_LIB)

AC_OUTPUT([
Makefile
src/Makefile
src/Mono.Nat/Makefile
src/Mono.Nat/mono-nat.pc
])

echo ""
echo "Configuration summary"
echo ""
echo "* Installation prefix : $prefix"
echo ""
