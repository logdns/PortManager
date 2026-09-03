use std::process::Command;

fn main() {
    let status = Command::new("wsl.exe")
        .args(std::env::args_os().skip(1))
        .status();

    match status {
        Ok(value) => std::process::exit(value.code().unwrap_or(1)),
        Err(error) => {
            eprintln!("Could not start wsl.exe: {error}");
            std::process::exit(1);
        }
    }
}
