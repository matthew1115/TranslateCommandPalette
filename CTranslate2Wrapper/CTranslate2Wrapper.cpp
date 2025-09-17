#include "pch.h"
#include "CTranslate2Wrapper.h"

// Required C++ standard library headers
#include <sstream>
#include <vector>
#include <string>

// CTranslate2 and C++/CLI interop headers
#include <ctranslate2/translator.h>
#include <msclr/marshal_cppstd.h>

// Forward declaration for the callback wrapper
class CallbackWrapper;

// This is the Private Implementation (PImpl) idiom.
// It hides the native C++ types from the header file, which improves compile times
// and prevents issues with including native headers in C# projects.
class CTranslate2WrapperImpl {
public:
    // This holds the pointer to the actual CTranslate2 engine.
    std::unique_ptr<ctranslate2::Translator> translator;
    // Store the callback wrapper to keep it alive during translation
    std::unique_ptr<CallbackWrapper> callbackWrapper;
};

// Native C++ class to wrap the managed callback
class CallbackWrapper {
private:
    gcroot<CTranslate2Wrapper::TranslationCallback^> managedCallback;

public:
    CallbackWrapper(CTranslate2Wrapper::TranslationCallback^ callback) 
        : managedCallback(callback) {}

    bool callCallback(const ctranslate2::GenerationStepResult& step) {
        // Check if the managed callback is valid
        CTranslate2Wrapper::TranslationCallback^ callback = managedCallback;
        if (callback != nullptr) {
            return callback(static_cast<int>(step.step));
        }
        return false;
    }

    // Static function that can be used with std::function
    static CallbackWrapper* currentWrapper;
    static bool staticCallback(const ctranslate2::GenerationStepResult& step) {
        if (currentWrapper != nullptr) {
            return currentWrapper->callCallback(step);
        }
        return false;
    }
};

// Static member definition
CallbackWrapper* CallbackWrapper::currentWrapper = nullptr;

// Use the namespace defined in your header file
using namespace CTranslate2Wrapper;

// Constructor: Initializes the native translator engine.
Translator::Translator(String^ modelPath) {
    m_pImpl = new CTranslate2WrapperImpl();
    try
    {
        // Convert managed string to native and construct translator
        std::string nativeModelPath = msclr::interop::marshal_as<std::string>(modelPath);
        m_pImpl->translator = std::make_unique<ctranslate2::Translator>(nativeModelPath, ctranslate2::Device::CPU);
    }
    catch (const std::exception& e)
    {
        // Emit to OutputDebugString so the message appears in DebugView / VS Output (Debug -> Windows -> Output)
        std::string msg = "CTranslate2Wrapper ctor failed: ";
        msg += e.what();
        msg += "\n";
        OutputDebugStringA(msg.c_str());

        delete m_pImpl;
        m_pImpl = nullptr;
        throw gcnew Exception(msclr::interop::marshal_as<String^>(e.what()));
    }
}

// Original Translate Method: This maintains backward compatibility.
String^ Translator::Translate(String^ text) {
    return Translate(text, nullptr);
}

// Translate Method with Options: This supports cancellation via callback.
String^ Translator::Translate(String^ text, TranslationOptions^ options) {
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

    // 4. Set up translation options with callback support
    ctranslate2::TranslationOptions native_options;
    
    // If options are provided and callback is set, wrap the managed callback
    if (options != nullptr && options->callback != nullptr) {
        // Create a native callback wrapper that can be used with CTranslate2
        m_pImpl->callbackWrapper = std::unique_ptr<CallbackWrapper>(new CallbackWrapper(options->callback));
        
        // Set the static pointer and assign the callback
        CallbackWrapper::currentWrapper = m_pImpl->callbackWrapper.get();
        native_options.callback = CallbackWrapper::staticCallback;
    }

    // 5. Call the CTranslate2 engine with options
    const std::vector<ctranslate2::TranslationResult> results = 
        m_pImpl->translator->translate_batch(batch_tokens, native_options);

    // Clean up the callback wrapper after translation
    CallbackWrapper::currentWrapper = nullptr;
    m_pImpl->callbackWrapper.reset();

    if (results.empty())
    {
        return String::Empty;
    }

    // 6. Manually join the output tokens into a single string.
    const std::vector<std::string>& output_tokens = results[0].output();
    std::string translatedText;
    for (size_t i = 0; i < output_tokens.size(); ++i) {
        translatedText += output_tokens[i];
        // Add a space between tokens, but not after the last one.
        if (i < output_tokens.size() - 1) {
            translatedText += " ";
        }
    }

    // 7. Marshal the native C++ string result back to a .NET string and return it.
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