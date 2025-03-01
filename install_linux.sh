#!/bin/bash
# Installation script for the simple shell on Linux systems

# Exit on error
set -e

# Default installation directory
INSTALL_DIR="/usr/local/bin"
SHELL_NAME="hsh"
SHELL_PATH="$INSTALL_DIR/$SHELL_NAME"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[0;33m'
NC='\033[0m' # No Color

# Print usage
usage() {
    echo "Usage: $0 [OPTIONS]"
    echo "Install the simple shell on your system."
    echo ""
    echo "Options:"
    echo "  --prefix=DIR       Install binaries in DIR (default: /usr/local/bin)"
    echo "  --set-default      Set as default shell for current user"
    echo "  --help             Display this help and exit"
    echo ""
}

# Parse command line arguments
for arg in "$@"; do
    case $arg in
        --prefix=*)
            INSTALL_DIR="${arg#*=}"
            SHELL_PATH="$INSTALL_DIR/$SHELL_NAME"
            shift
            ;;
        --set-default)
            SET_DEFAULT=1
            shift
            ;;
        --help)
            usage
            exit 0
            ;;
        *)
            echo -e "${RED}Unknown option: $arg${NC}"
            usage
            exit 1
            ;;
    esac
done

# Check if build directory exists
if [ ! -d "build" ]; then
    echo -e "${YELLOW}Build directory not found. Creating and building...${NC}"
    mkdir -p build
    cd build
    cmake .. -DBUILD_STATIC=ON
    make
    cd ..
fi

# Check if the binary exists
if [ ! -f "build/bin/$SHELL_NAME" ]; then
    echo -e "${RED}Binary not found. Please build the project first.${NC}"
    echo "Run: mkdir -p build && cd build && cmake .. && make"
    exit 1
fi

# Create installation directory if it doesn't exist
if [ ! -d "$INSTALL_DIR" ]; then
    echo -e "${YELLOW}Creating installation directory: $INSTALL_DIR${NC}"
    sudo mkdir -p "$INSTALL_DIR"
fi

# Install the shell
echo -e "${GREEN}Installing $SHELL_NAME to $SHELL_PATH${NC}"
sudo cp "build/bin/$SHELL_NAME" "$SHELL_PATH"
sudo chmod 755 "$SHELL_PATH"

# Add to /etc/shells if not already there
if ! grep -q "^$SHELL_PATH$" /etc/shells; then
    echo -e "${YELLOW}Adding $SHELL_PATH to /etc/shells${NC}"
    echo "$SHELL_PATH" | sudo tee -a /etc/shells > /dev/null
fi

# Set as default shell if requested
if [ "$SET_DEFAULT" = "1" ]; then
    echo -e "${YELLOW}Setting $SHELL_NAME as default shell for $(whoami)${NC}"
    chsh -s "$SHELL_PATH"
    echo -e "${GREEN}Default shell changed. Please log out and log back in for changes to take effect.${NC}"
fi

echo -e "${GREEN}Installation complete!${NC}"
echo "You can run the shell by typing: $SHELL_NAME"
if [ "$SET_DEFAULT" != "1" ]; then
    echo "To set as default shell, run: chsh -s $SHELL_PATH"
fi

exit 0 