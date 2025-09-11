#include "pch.h"
#include "CTranslate2Wrapper.h"

// Required C++ standard library headers
#include <sstream>
#include <vector>
#include <string>

// CTranslate2 and C++/CLI interop headers
#include <ctranslate2/translator.h>
#include <msclr/marshal_cppstd.h>

// This is the Private Implementation (PImpl) idiom.
// It hides the native C++ types from the header file, which improves compile times
// and prevents issues with including native headers in C# projects.
class CTranslate2WrapperImpl {
public:
    // This holds the pointer to the actual CTranslate2 engine.
    std::unique_ptr<ctranslate2::Translator> translator;
};

// Use the namespace defined in your header file
using namespace CTranslate2Wrapper;

// Constructor: Initializes the native translator engine.
Translator::Translator(String^ modelPath) {
    m_pImpl = new CTranslate2WrapperImpl();
    try
    {
        // Convert the managed .NET string to a native C++ std::string.
        std::string nativeModelPath = msclr::interop::marshal_as<std::string>(modelPath);

        // Create the native CTranslate2 Translator object.
        m_pImpl->translator = std::make_unique<ctranslate2::Translator>(nativeModelPath, ctranslate2::Device::CPU);
    }
    catch (const std::exception& e)
    {
        // If the native code throws an exception (e.g., model not found),
        // clean up and re-throw it as a managed exception that C# can catch.
        delete m_pImpl;
        m_pImpl = nullptr;
        throw gcnew Exception(msclr::interop::marshal_as<String^>(e.what()));
    }
}

// Translate Method: This is the core function your C# app will call.
String^ Translator::Translate(String^ text) {
    if (m_pImpl == nullptr)
    {
        throw gcnew ObjectDisposedException("Translator instance has been disposed.");
    }

    // 1. Marshal (convert) the input .NET string to a native C++ string.
    std::string nativeText = msclr::interop::marshal_as<std::string>(text);

    // 2. Tokenize the input string. This is a simple split by space.
    std::vector<std::string> tokens;
    std::istringstream iss(nativeText);
    for (std::string s; iss >> s;)
    {
        tokens.push_back(s);
    }

    if (tokens.empty())
    {
        return String::Empty;
    }

    // 3. The translate_batch method expects a vector of sentences.
    //    We wrap our single sentence's tokens in another vector to create a batch of one.
    std::vector<std::vector<std::string>> batch_tokens = { tokens };

    // 4. Call the CTranslate2 engine.
    const std::vector<ctranslate2::TranslationResult> results = m_pImpl->translator->translate_batch(batch_tokens);

    if (results.empty())
    {
        return String::Empty;
    }

    // 5. FIX: Manually join the output tokens into a single string.
    const std::vector<std::string>& output_tokens = results[0].output();
    std::string translatedText;
    for (size_t i = 0; i < output_tokens.size(); ++i) {
        translatedText += output_tokens[i];
        // Add a space between tokens, but not after the last one.
        if (i < output_tokens.size() - 1) {
            translatedText += " ";
        }
    }

    // 6. Marshal the native C++ string result back to a .NET string and return it.
    return msclr::interop::marshal_as<String^>(translatedText);
}

// This is the IDisposable pattern for C++/CLI.
// The destructor (~), called by C#'s 'using' block, chains to the finalizer (!).
Translator::~Translator() {
    this->!Translator();
}

// The finalizer is the last line of defense to clean up unmanaged resources.
Translator::!Translator() {
    if (m_pImpl != nullptr) {
        delete m_pImpl;
        m_pImpl = nullptr;
    }
}