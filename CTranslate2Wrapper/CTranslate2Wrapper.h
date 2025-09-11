#pragma once

class CTranslate2WrapperImpl; // Forward declaration
using namespace System;

namespace CTranslate2Wrapper {
    public ref class Translator : IDisposable
    {
    public:
        Translator(String^ modelPath);
        ~Translator(); // Destructor
        !Translator(); // Finalizer

        String^ Translate(String^ text);

    private:
        CTranslate2WrapperImpl* m_pImpl;
    };
}