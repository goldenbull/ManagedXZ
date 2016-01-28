# About ManagedXZ

this is a simple .net wrapper for xz utils from http://tukaani.org/xz/

# something about Native and .NET interop

There are many tools/packages like xz written in C/C++ and we hope to use them in C# world. There are several ways to achieve this goal.

1. RPC

"Render therefore unto Caesar the things which are Caesar's; and unto God the things that are God's."
Some RPC frameworks like thrift, avro and gRPC(https://github.com/grpc/grpc) are fundamental cornerstones of a large scale product. One language is suitable for one scenario the the other language is for the other. And RPC provides the ability of cross language access. With the help of RPC, everything is service. A beautiful and practical way to write realworld applications.
But it's sometimes too heavy for a simple tool library and has too many dependencies.

2. C++/CLI

Formerly it's called 'Managed C++'. Microsoft invented this language to make it possible to use C/C++ code directly, or, without a lot of modifications in .net world. It's quite useful in some cases. For example, a C++ library has a virtual base class, and the user have to implement their own derived class, and then send the pointer of the derived class to a function in the library to do some work. It's quite difficult and error-prone to deal with the native C++ class in C# code, so it's better to use C++/CLI to connect the two parts together.
If the third-party package provides source code or .lib and .h files, the generated dll assembly will contain both native and managed code. But it must be platform-specific, x86 or x64, can not be AnyCPU, unhappy.

3. DllImport

If the package is provided in the form of one or more dll files, we can use DllImport to dynamically load them on runtime. We also have to translate the needed native structs into corresponding C# struct(or class) using StructLayout. This way is the most 'clean' one because it will not change the original package.

# So what's my choice?

Think about the two facts:
* xz is a fundamental useful tool in varies scenarios: data files, logs, images, HTTP response, ... So the managed wrapper is better to be AnyCPU
* xz dll interface is simple, there is no need to deal with the callbacks, derived classes, etc.
So, it seems quite reasonable to use DllImport to access liblzma.dll :smile: 