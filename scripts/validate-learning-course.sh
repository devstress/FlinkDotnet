#!/bin/bash

# LearningCourse Documentation Validation Script
# Tests beginner-friendliness criteria across all days

set -e

echo "🧪 LearningCourse Documentation Validation"
echo "=========================================="

LEARNING_COURSE_DIR="LearningCourse"
TOTAL_DAYS=0
PASSED_DAYS=0
FAILED_DAYS=0

declare -A ISSUES

# Function to check if a day meets beginner-friendly criteria
check_day_criteria() {
    local day_dir="$1"
    local readme_file="$day_dir/Exercise-Solutions/README.md"
    local day_name=$(basename "$day_dir")
    
    echo "📋 Checking $day_name..."
    
    if [ ! -f "$readme_file" ]; then
        ISSUES["$day_name"]+="❌ Missing README.md; "
        return 1
    fi
    
    local score=0
    local total_criteria=6
    
    # 1. Check for QUICK START section
    if grep -q "QUICK START\|Students:" "$readme_file"; then
        score=$((score + 1))
        echo "  ✅ Has QUICK START section"
    else
        ISSUES["$day_name"]+="❌ Missing QUICK START section; "
        echo "  ❌ Missing QUICK START section"
    fi
    
    # 2. Check for Prerequisites section  
    if grep -q "Prerequisites\|MUST DO FIRST" "$readme_file"; then
        score=$((score + 1))
        echo "  ✅ Has Prerequisites section"
    else
        ISSUES["$day_name"]+="❌ Missing Prerequisites section; "
        echo "  ❌ Missing Prerequisites section"
    fi
    
    # 3. Check for Step-by-Step exercises
    if grep -q "Step-by-Step\|Exercise [0-9]" "$readme_file"; then
        score=$((score + 1))
        echo "  ✅ Has Step-by-Step exercises"
    else
        ISSUES["$day_name"]+="❌ Missing Step-by-Step exercises; "
        echo "  ❌ Missing Step-by-Step exercises"
    fi
    
    # 4. Check for copy/paste commands (bash code blocks)
    if grep -q "\`\`\`bash" "$readme_file"; then
        score=$((score + 1))
        echo "  ✅ Has copy/paste commands"
    else
        ISSUES["$day_name"]+="❌ Missing copy/paste commands; "
        echo "  ❌ Missing copy/paste commands"
    fi
    
    # 5. Check for success indicators/expected output
    if grep -q "Expected Output\|Success indicators\|✅" "$readme_file"; then
        score=$((score + 1))
        echo "  ✅ Has success indicators"
    else
        ISSUES["$day_name"]+="❌ Missing success indicators; "
        echo "  ❌ Missing success indicators"
    fi
    
    # 6. Check for infrastructure verification (LocalTesting)
    if grep -q "LocalTesting\|infrastructure\|curl.*localhost" "$readme_file"; then
        score=$((score + 1))
        echo "  ✅ Has infrastructure verification"
    else
        ISSUES["$day_name"]+="❌ Missing infrastructure verification; "
        echo "  ❌ Missing infrastructure verification"
    fi
    
    local percentage=$((score * 100 / total_criteria))
    echo "  📊 Score: $score/$total_criteria ($percentage%)"
    
    if [ $score -ge 5 ]; then
        echo "  🎉 PASSED - Beginner-friendly"
        return 0
    else
        echo "  💔 FAILED - Needs improvement"
        return 1
    fi
}

# Main validation loop
if [ ! -d "$LEARNING_COURSE_DIR" ]; then
    echo "❌ LearningCourse directory not found!"
    exit 1
fi

for day_dir in "$LEARNING_COURSE_DIR"/Day*; do
    if [ -d "$day_dir" ]; then
        TOTAL_DAYS=$((TOTAL_DAYS + 1))
        
        if check_day_criteria "$day_dir"; then
            PASSED_DAYS=$((PASSED_DAYS + 1))
        else
            FAILED_DAYS=$((FAILED_DAYS + 1))
        fi
        echo
    fi
done

# Summary report
echo "📊 VALIDATION SUMMARY"
echo "===================="
echo "📚 Total Days: $TOTAL_DAYS"
echo "✅ Passed: $PASSED_DAYS"
echo "❌ Failed: $FAILED_DAYS"
echo "📈 Success Rate: $((PASSED_DAYS * 100 / TOTAL_DAYS))%"
echo

if [ $FAILED_DAYS -gt 0 ]; then
    echo "🚨 ISSUES FOUND:"
    echo "================="
    for day in "${!ISSUES[@]}"; do
        echo "📘 $day:"
        echo "   ${ISSUES[$day]}"
    done
    echo
    echo "💡 RECOMMENDATIONS:"
    echo "- Standardize documentation structure across all days"
    echo "- Add missing QUICK START sections where needed"
    echo "- Ensure all days have clear Prerequisites sections"
    echo "- Include infrastructure verification steps"
    echo "- Add success indicators for better beginner experience"
fi

if [ $FAILED_DAYS -eq 0 ]; then
    echo "🎉 ALL DAYS PASSED! LearningCourse is beginner-friendly!"
    exit 0
else
    echo "⚠️  Some days need improvement for beginner-friendliness"
    exit 1
fi