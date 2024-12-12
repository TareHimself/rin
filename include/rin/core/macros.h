#pragma once

#define RIN_MACRO_JOIN_IMPL(A,B) A##B

#define RIN_MACRO_JOIN(A,B) RIN_MACRO_JOIN_IMPL(A,B)
