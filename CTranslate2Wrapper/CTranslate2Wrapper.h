#pragma once

class CTranslate2WrapperImpl; // Forward declaration
using namespace System;
using namespace System::Threading;

namespace CTranslate2Wrapper {
    // Delegate for the translation callback function
    public delegate bool TranslationCallback(int step);

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