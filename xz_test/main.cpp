#include <iostream>
#include <string>
#include <vector>
#include <memory>
#include "lzma.h"
using namespace std;

#pragma comment(lib, "../api/lib/liblzma.lib")

int main()
{
    lzma_stream stream;
    cout << sizeof(lzma_stream) << endl;
    cout << sizeof(lzma_mt) << endl;
    return 0;
}