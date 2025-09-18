#pragma once

class CTranslate2WrapperImpl; // Forward declaration
using namespace System;
using namespace System::Threading;

namespace CTranslate2Wrapper {
    // Delegate for the translation callback function
    public delegate bool TranslationCallback(int step);

    // Managed wrapper for translation options
    public ref class TranslationOptions {
    public:
        TranslationCallback^ callback;
        
        TranslationOptions() : callback(nullptr) {}
    };

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