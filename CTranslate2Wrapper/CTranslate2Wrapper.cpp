#include "pch.h"
#include "CTranslate2Wrapper.h"

// Required C++ standard library headers
#include <vector>
#include <string>
#include <memory>

// CTranslate2, sentencepiece and C++/CLI interop headers
#include <ctranslate2/translator.h>
#include <sentencepiece_processor.h>

// Add these includes for UTF-8/UTF-16 conversion
#include <msclr/marshal.h>
#include <msclr/marshal_cppstd.h>
#include <codecvt>

// Convert System::String^ (UTF-16) → std::string (UTF-8)
std::string toUtf8(System::String^ s) {
    using convert_t = std::wstring_convert<std::codecvt_utf8_utf16<wchar_t>>;
    convert_t converter;
    std::wstring wstr = msclr::interop::marshal_as<std::wstring>(s);
    return converter.to_bytes(wstr);
}

// Convert std::string (UTF-8) → System::String^ (UTF-16)
System::String^ fromUtf8(const std::string& s) {
    using convert_t = std::wstring_convert<std::codecvt_utf8_utf16<wchar_t>>;
    convert_t converter;
    std::wstring wstr = converter.from_bytes(s);
    return gcnew System::String(wstr.c_str());
}

// This is the Private Implementation (PImpl) idiom.
// It hides the native C++ types from the header file, which improves compile times
// and prevents issues with including native headers in C# projects.
class CTranslate2WrapperImpl {
public:
	std::unique_ptr<std::string> nativeModelPath;
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
		m_pImpl->nativeModelPath = std::make_unique<std::string>(msclr::interop::marshal_as<std::string>(modelPath));

		// Create the native CTranslate2 Translator object.
		const std::vector<int> device_indices = { 0 };
		m_pImpl->translator = std::make_unique<ctranslate2::Translator>(*(m_pImpl->nativeModelPath), ctranslate2::Device::CPU, ctranslate2::ComputeType::INT8, device_indices);

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

	// Load the SentencePiece spm file.
	sentencepiece::SentencePieceProcessor tokenizer;
	// std::string sp_model_path = *(m_pImpl->nativeModelPath) + "/source.spm";
	std::string sp_model_path = "D:/coding/TranslateCommandPalette/TranslateCommandPalette/Models/opus_en_zh_ct2_int8/source.spm";
	std::string debugMsg = "Trying to load SentencePiece model from: " + sp_model_path + "\n";
	OutputDebugStringA(debugMsg.c_str());
	try {
		const auto sp_status = tokenizer.Load(sp_model_path);
		if (!sp_status.ok()) {
			throw std::runtime_error("Failed to load SentencePiece model: " + sp_status.ToString());
		}
	}
	catch (const std::exception& e) {
		OutputDebugStringA("Exception caught while loading SentencePiece model.\n");
		throw gcnew Exception(msclr::interop::marshal_as<String^>(e.what()));
	}

	// 1. Marshal (convert) the input .NET string to a native C++ string.
	std::string nativeText = toUtf8(text);

	// 2. Tokenize the input string. Use sentencepiece
	std::vector<std::string> tokens;
	tokenizer.Encode(nativeText, &tokens);
	// Add BOS and EOS tokens
	tokens.insert(tokens.begin(), "<s>");
	tokens.push_back("</s>");


	// 3. The translate_batch method expects a vector of sentences.
	//    We wrap our single sentence's tokens in another vector to create a batch of one.
	std::vector<std::vector<std::string>> batch_tokens = { tokens };

	// 4. Call the CTranslate2 engine.
	const std::vector<ctranslate2::TranslationResult> results = m_pImpl->translator->translate_batch(batch_tokens);

	if (results.empty())
	{
		return String::Empty;
	}


	const std::vector<std::string>& output_tokens = results[0].output();
	std::string translatedText;
	auto status = tokenizer.Decode(output_tokens, &translatedText);
	if (!status.ok()) {
		throw gcnew Exception(msclr::interop::marshal_as<String^>(
			"Failed to decode SentencePiece tokens: " + status.ToString()));
	}

	// 6. Marshal the native C++ string result back to a .NET string and return it.
	return fromUtf8(translatedText);
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