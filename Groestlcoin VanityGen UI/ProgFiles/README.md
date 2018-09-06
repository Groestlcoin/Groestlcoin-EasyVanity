A standalone command line vanity address generator called vanitygen.

Vanitygen is written in C, and is provided in source code form and 
pre-built Win32 binaries.  At present, vanitygen can be built on Linux, 
and requires the openssl and pcre libraries.

Vanitygen can generate regular groestlcoin addresses and testnet addresses.

Vanitygen can search for exact prefixes or regular expression matches.  
When searching for exact prefixes, vanitygen will ensure that the 
prefix is possible, will provide a difficulty estimate, and will run 
about 30% faster.  Exact prefixes are case-sensitive by default, but 
may be searched case-insensitively using the "-i" option.  Regular 
expression patterns follow the Perl-compatible regular expression 
language.

Vanitygen can accept a list of patterns to search for, either on the 
command line, or from a file or stdin using the "-f" option.  File 
sources should have one pattern per line.  When searching for N exact 
prefixes, performance of O(logN) can be expected, and extremely long 
lists of prefixes will have little effect on search rate.  Searching 
for N regular expressions will have varied performance depending on the 
complexity of the expressions, but O(N) performance can be expected.

By default, vanitygen will spawn one worker thread for each CPU in your 
system.  If you wish to limit the number of worker threads created by 
vanitygen, use the "-t" option.

-----
Latest binary release is available at: https://github.com/Groestlcoin/vanitygen/releases
-----

###General Usage Example:###

**Generate an address using CPU(slower):**  

*Linux: ./vanitygen Fgrs*  
*Windows: vanitygen.exe Fgrs*  
>Difficulty: 78508  
>Pattern: Fgrs                                                                  
>Address: Fgrsa16NshK1ua6KyBSXahz6D9PYUbvL3d  
>Privkey: 5JssG8to6x62vf9pC7ktJhXc3jJV31JR1Do7qLvQEx8wVUQ37op  

**Generate an address using GPU(faster):**  

*Linux: ./oclvanitygen Fgrs*  
*Windows: oclvanitygen.exe Fgrs*  
>Difficulty: 78508  
>Pattern: Fgrs                                                                    
>Address: FgrsjEdkYaubKoxqKJ9uGP9pHxknhrn7Vj  
>Privkey: 5KHCnR5HAkdrePoQiXcvJh3ZADt1EymnryySi4tkqTArnQGTwQc  

-----

###Password protecting private keys:###

*Linux: ./vanitygen -o results.txt -i -e Fgrs*  
*Windows: vanitygen.exe -o results.txt -i -e Fgrs*  

For GPU use substitute "vanitygen" with "oclvanitygen" in command.  
"-o results.txt" save results to text file results.txt  
"-i" case insensitive  
"-e" prompt for password(used to encrypt/decrypt private key)  
"Fgrs" Pattern to search for  
>Difficulty: 19627  
>Enter new password:  
>Verifying - Enter new password:  
>Pattern: Fgrs                                                                   
>Address: FgRsguD6fqcHM7HXHG6WDqSg78NrdRB4Bz  
>Protkey: PsTaTXeXSLDrVc8bjJVg6uTvDhdziG5jHXyM6jC1j1wf4EFCt5n5djP5rBbtnztkvQLQ  

-----

###Decrypting password protected private keys:###

*Linux: ./keyconv -d PsTaTXeXSLDrVc8bjJVg6uTvDhdziG5jHXyM6jC1j1wf4EFCt5n5djP5rBbtnztkvQLQ*  
*Windows: keyconv.exe -d PsTaTXeXSLDrVc8bjJVg6uTvDhdziG5jHXyM6jC1j1wf4EFCt5n5djP5rBbtnztkvQLQ*  

"-d" decrypt flag  
"PsTaTXeXSLDrVc8bjJVg6uTvDhdziG5jHXyM6jC1j1wf4EFCt5n5djP5rBbtnztkvQLQ" private key to decrypt  
>Enter import password:  
>Address: FgRsguD6fqcHM7HXHG6WDqSg78NrdRB4Bz  
>Privkey: 5JvKJy4XSeGRHapyTich5oVYiwANPbzjvFiHXSafawU4eNcD5Wt  