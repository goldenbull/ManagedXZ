#include <iostream>
#include <string>
#include <vector>
#include <memory>
#include "lzma.h"
using namespace std;

#pragma comment(lib, "../api/lib/liblzma.lib")

int main()
{
    lzma_stream stream = LZMA_STREAM_INIT;
    cout << sizeof(lzma_stream) << endl;
    cout << sizeof(lzma_mt) << endl;
    lzma_end(&stream);
    lzma_end(&stream);
    lzma_end(&stream);
    return 0;
}